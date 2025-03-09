#include "stdafx.h"

auto getExecutablePath()
{
    char executablePath[PATH_MAX];
    uint32_t buffersize = PATH_MAX;
    if (!_NSGetExecutablePath(executablePath, &buffersize))
    {
        return std::filesystem::path(executablePath).remove_filename();
    }
}

int main(int, char**)
{
    const std::string& dataFileName = "foobar.dat";
    auto executablePath = getExecutablePath();
    auto dataFilePath = executablePath;
    dataFilePath += dataFileName;

    std::ifstream dataFile(dataFilePath);
    if (dataFile.is_open())
    {
        std::cout << dataFile.rdbuf();
        dataFile.close();
    }
    else
    {
        std::cout << "Error: " << dataFileName << " not found near the executable in path: " << executablePath << std::endl;
        return 1;
    }
    return 0;
}
