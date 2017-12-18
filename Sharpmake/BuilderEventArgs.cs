using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharpmake
{
    /// <summary>
    /// Event arguments about generated projects and solutions.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="Builder.EventPreGeneration"/> and
    /// <see cref="Builder.EventPostGeneration"/>.
    /// </remarks>
    public class GenerationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the list of <see cref="Solution"/> generated.
        /// </summary>
        public IReadOnlyList<Solution> Solutions { get; }

        /// <summary>
        /// Gets the list of <see cref="Project"/> generated.
        /// </summary>
        public IReadOnlyList<Project> Projects { get; }

        /// <summary>
        /// Gets a dictionary that maps the <see cref="Type"/> of a project and a <see cref="GenerationOutput"/> that enables  generation output for a given project type.
        /// </summary>
        public IDictionary<Type, GenerationOutput> Report { get; }

        internal GenerationEventArgs(IEnumerable<Solution> solutions, IEnumerable<Project> projects, IDictionary<Type, GenerationOutput> report)
        {
            Solutions = solutions.ToArray();
            Projects = projects.ToArray();
            Report = report;
        }
    }

    /// <summary>
    /// Generic event arguments for events involving a project.
    /// </summary>
    public class ProjectEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="Sharpmake.Project"/> involved in the raised event.
        /// </summary>
        public Project Project { get; }

        internal ProjectEventArgs(Project project)
        {
            Project = project;
        }
    }

    /// <summary>
    /// Generic event arguments for events involving a project configuration.
    /// </summary>
    public class ProjectConfigurationEventArgs : ProjectEventArgs
    {
        /// <summary>
        /// Gets the <see cref="Project.Configuration"/> involved in the raised event.
        /// </summary>
        public Project.Configuration Configuration { get; }

        internal ProjectConfigurationEventArgs(Project project, Project.Configuration configuration)
            : base(project)
        {
            Configuration = configuration;
        }
    }

    /// <summary>
    /// Generic event arguments for events involving a solution.
    /// </summary>
    public class SolutionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="Sharpmake.Solution"/> involved in the raised event.
        /// </summary>
        public Solution Solution { get; }

        internal SolutionEventArgs(Solution solution)
        {
            Solution = solution;
        }
    }

    /// <summary>
    /// Generic event arguments for events involving a solution configuration.
    /// </summary>
    public class SolutionConfigurationEventArgs : SolutionEventArgs
    {
        /// <summary>
        /// Gets the <see cref="Solution.Configuration"/> involved in the raised event.
        /// </summary>
        public Solution.Configuration Configuration { get; }

        internal SolutionConfigurationEventArgs(Solution solution, Solution.Configuration configuration)
            : base(solution)
        {
            Configuration = configuration;
        }
    }
}
