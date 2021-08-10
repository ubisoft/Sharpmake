#include <curl/curl.h>
#include <rapidjson/rapidjson.h>
#include <rapidjson/document.h>
#include <rapidjson/stringbuffer.h>
#include <rapidjson/writer.h>
#include <iostream>

int main(int argc, char** argv)
{
    CURL* curlHandle = curl_easy_init();
    // TODO... do some stuff
    curl_easy_cleanup(curlHandle);


    // Do some useless stuff with rapidjson
    rapidjson::Document jsonDoc;
    jsonDoc.SetArray();
    rapidjson::Value newSection(rapidjson::kObjectType);
    newSection.AddMember("count", 0, jsonDoc.GetAllocator());
    jsonDoc.PushBack(newSection, jsonDoc.GetAllocator());

    rapidjson::StringBuffer buffer;

    buffer.Clear();

    rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
    jsonDoc.Accept(writer);

    std::cout << "Here is a JSON from an exe built in "

#if _DEBUG
        "Debug"
#endif

#if NDEBUG
        "Release"
#endif

     ":\n" << buffer.GetString() << std::endl;

    return 0;
}
