#include <curl/curl.h>
#include <rapidjson/rapidjson.h>
#include <rapidjson/document.h>


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
}
