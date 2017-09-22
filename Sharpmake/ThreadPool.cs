// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sharpmake
{
    // Very simple task scheduler, 
    //  support:
    //      - adding task from inside another task
    //      - Wait(), wait for all tasks
    //      - Stop(), wait for all tasks and stop
    public class ThreadPool : IDisposable
    {
        public enum Priority
        {
            High = 0,
            Normal = 1,
            Low = 2,
            Count
        }

        public delegate void TaskCallback(object parameters);

        public class TaskQueue
        {
            private readonly Queue<Task>[] _tasks = new Queue<Task>[(int)Priority.Count];

            public TaskQueue()
            {
                for (int i = 0; i < (int)Priority.Count; ++i)
                    _tasks[i] = new Queue<Task>();
            }

            public int Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < (int)Priority.Count; ++i)
                        count += _tasks[i].Count;
                    return count;
                }
            }

            public void Enqueue(Task task, Priority priority = Priority.Normal)
            {
                _tasks[(int)priority].Enqueue(task);
            }

            public Task Dequeue()
            {
                for (int i = 0; i < (int)Priority.Count; ++i)
                {
                    if (_tasks[i].Count != 0)
                        return _tasks[i].Dequeue();
                }
                return null;
            }
        };

        public class Task
        {
            private readonly TaskCallback _taskCallback;
            private readonly object _parameter;

            internal Task(TaskCallback taskCallback, object parameter)
            {
                _taskCallback = taskCallback;
                _parameter = parameter;
            }

            internal void Run()
            {
                _taskCallback(_parameter);
            }
        }

        private Semaphore _taskAvailable;
        private ManualResetEvent _eventStop;
        private ManualResetEvent _eventIdle;
        private Exception _unHandledException = null;

        private bool _started = false;
        private Thread[] _thread;
        private readonly TaskQueue _tasks = new TaskQueue();
        private int _workTask = 0;

        public void AddTask(TaskCallback callback, Priority priority = Priority.Normal)
        {
            AddTask(callback, null);
        }

        public void AddTask(TaskCallback callback, object parameter, Priority priority = Priority.Normal)
        {
            Task task = new Task(callback, parameter);

            lock (_tasks)
            {
                Debug.Assert(_started);
                _tasks.Enqueue(task);
                _eventIdle.Reset();
                _taskAvailable.Release();
            }
        }

        public void Start()
        {
            Start(Environment.ProcessorCount);
        }

        private object _locker = new object();
        public void Start(int numThread)
        {
            lock (_locker)
            {
                Debug.Assert(_started == false);
                Debug.Assert(numThread != 0);

                _started = true;
                _taskAvailable = new Semaphore(0, int.MaxValue);
                _eventStop = new ManualResetEvent(false);
                _eventIdle = new ManualResetEvent(true);
                _thread = new Thread[numThread];
                for (int i = 0; i < numThread; ++i)
                {
                    _thread[i] = new Thread(ThreadWork);
                    _thread[i].Name = String.Format("Tasks Thread #{0,3}", i);
                    _thread[i].Start(i);
                }
            }
        }

        public int NumTasks() { return _thread.Length; }

        public void Stop()
        {
            lock (_locker)
            {
                Wait();
                _eventStop.Set();
                for (int i = 0; i < _thread.Length; ++i)
                    _thread[i].Join();
                _started = false;
            }
        }

        public void Wait()
        {
            lock (_locker)
            {
                Debug.Assert(_started);
                _eventIdle.WaitOne();

                if (_unHandledException != null)
                {
                    Exception e = _unHandledException;
                    _unHandledException = null;
                    if (e is Error)
                        throw new Error(e, "Unhandled exception in thread pool");
                    if (e is InternalError)
                        throw new InternalError(e, "Unhandled exception in thread pool");
                    throw new Exception("Unhandled exception in thread pool", e);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void ThreadWork(object obj)
        {
            WaitHandle[] waitHandles = { _taskAvailable, _eventStop };

            while (true)
            {
                int index = WaitHandle.WaitAny(waitHandles);

                if (waitHandles[index] == _eventStop)
                    return;

                Task task;

                lock (_tasks)
                {
                    Debug.Assert(_tasks.Count != 0);
                    task = _tasks.Dequeue();
                    _workTask++;
                }

                try
                {
                    task.Run();
                }
                catch (Exception e)
                {
                    // keep the first exception
                    Interlocked.CompareExchange(ref _unHandledException, e, null);
                }

                lock (_tasks)
                {
                    _workTask--;
                    if (_tasks.Count == 0 && _workTask == 0)
                        _eventIdle.Set();
                }
            }
        }
    }
}
