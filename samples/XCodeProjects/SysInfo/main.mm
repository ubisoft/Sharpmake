#include <string>
#include <unistd.h>
#include <ifaddrs.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <net/if.h>
#include <sys/utsname.h>
#include <sys/sysctl.h>

#import <fmt/core.h>
#import "Globals.h"

#import <AVFoundation/AVFoundation.h>
#import <DeviceCheck/DeviceCheck.h>
#import <IOKit/IOKitLib.h>

std::string ComputerName()
{
    return [[[NSProcessInfo processInfo] hostName] UTF8String];
}

std::string ComputerUserName()
{
#if IS_MAC_PLATFORM
    return [[[NSProcessInfo processInfo] userName] UTF8String];
#else
    return [[[UIDevice currentDevice] name] UTF8String];
#endif
}

std::string GetOSLanguage()
{
    // This is the first language in the Language & Region > Preferred Language Order list
    NSString* language = [[NSLocale preferredLanguages] objectAtIndex:0];
    return std::string([language UTF8String]);
}

std::string GetOSLocale()
{
    // This is the country specified by the user in the Language & Region > Region Fromats
    NSString* countryCode = [[NSLocale currentLocale] objectForKey:NSLocaleCountryCode];
    return std::string([countryCode UTF8String]);
}

std::string GetTimeZone()
{
    return [[[NSTimeZone localTimeZone] name] UTF8String];
}

std::string GetWifiAddress()
{
    std::string ret;

    struct ifaddrs *ifa_list;
    struct ifaddrs *ifa;
    char addrstr[INET6_ADDRSTRLEN], netmaskstr[INET6_ADDRSTRLEN];

    if (getifaddrs(&ifa_list) == 0)
    {
        uint32_t netmask = 0;
        for (ifa = ifa_list; ifa != NULL; ifa = ifa->ifa_next)
        {
            if (!ifa || (ifa->ifa_flags & (IFF_UP|IFF_RUNNING|IFF_LOOPBACK)) != (IFF_UP|IFF_RUNNING))
                continue;

            //MC_DEBUG("%s", ifa->ifa_name);
            //MC_DEBUG("  0x%.8x", ifa->ifa_flags);

            memset(addrstr, 0, sizeof(addrstr));
            memset(netmaskstr, 0, sizeof(netmaskstr));

            if (ifa->ifa_addr->sa_family == AF_INET)
            {
                inet_ntop(AF_INET, &((struct sockaddr_in *)ifa->ifa_addr)->sin_addr, addrstr, sizeof(addrstr));
                inet_ntop(AF_INET, &((struct sockaddr_in *)ifa->ifa_netmask)->sin_addr, netmaskstr, sizeof(netmaskstr));

                bool isLinkLocal = IN_LINKLOCAL(ntohl(((sockaddr_in *)ifa->ifa_addr)->sin_addr.s_addr));
                if (!isLinkLocal)
                {
                    ret = addrstr;
                }

                //MC_DEBUG("  IPv4: %s netmask %s %s", addrstr, netmaskstr, isLinkLocal ? "(LinkLocal)" : "");
            }
            else if (ifa->ifa_addr->sa_family == AF_INET6)
            {
                inet_ntop(AF_INET6, &((struct sockaddr_in6 *)ifa->ifa_addr)->sin6_addr, addrstr, sizeof(addrstr));
                inet_ntop(AF_INET6, &((struct sockaddr_in6 *)ifa->ifa_netmask)->sin6_addr, netmaskstr, sizeof(netmaskstr));

                bool isLinkLocal = IN6_IS_ADDR_LINKLOCAL(&((sockaddr_in6 *)ifa->ifa_addr)->sin6_addr);
                if (!isLinkLocal)
                {
                    ret = addrstr;
                }

                //MC_DEBUG("  IPv6: %s netmask %s %s", addrstr, netmaskstr, isLinkLocal ? "(LinkLocal)" : "");
            }
            else
            {
                //MC_DEBUG("  else: %d", ifa->ifa_addr->sa_family);
            }
        }
        freeifaddrs(ifa_list);
    }

    return ret;
}

std::string GetDeviceUUID()
{
    static std::string deviceUUID;
    // Cache the value as it apparently takes quite long to query it, especially the first time (> 1ms)
    if (deviceUUID.empty())
    {
        // alternative: get WiFi device MAC, send as base64
#if IS_MAC_PLATFORM
        __block std::string deviceUUID_block;
        [[DCDevice currentDevice] generateTokenWithCompletionHandler: ^(NSData* token, NSError * error){
            if (token)
            {
                NSString* uuid = [token base64EncodedStringWithOptions:0];
                deviceUUID_block = std::string([uuid UTF8String]);
            }
        }];
        deviceUUID = deviceUUID_block;
#else
        NSUUID* uuid = [[UIDevice currentDevice] identifierForVendor];
        deviceUUID = std::string([[uuid UUIDString] UTF8String]);
#endif
    }
    return deviceUUID;
}

float GetSystemVersion()
{
#if IS_MAC_PLATFORM
    if ([[NSProcessInfo processInfo] respondsToSelector:@selector(operatingSystemVersion)])
    {
        NSOperatingSystemVersion osSystemVersion = [[NSProcessInfo processInfo] operatingSystemVersion];

        return (float)osSystemVersion.majorVersion +
            (float)osSystemVersion.minorVersion * 0.1f +
            (float)osSystemVersion.patchVersion * 0.01f;
    }
#else
    return [[[UIDevice currentDevice] systemVersion] floatValue];
#endif
}

bool IsLocalNetworkPermissionGranted()
{
#if IS_MAC_PLATFORM
    if (GetSystemVersion() < 11.0) // This check is only for macOS 11+
        return true;
#else
    if (GetSystemVersion() < 13.9) // This check is only for macOS 11+
        return true;
#endif // IS_MAC_PLATFORM

    int err = 0;

    // send to myself discard service and then check the error code.
    std::string wifiAddrStr = GetWifiAddress();
    if (wifiAddrStr.find('.') > 0)
    {
        int sock = socket(AF_INET, SOCK_DGRAM, 0);
        if (sock >= 0)
        {
            sockaddr_in addr = {0};
            addr.sin_family = AF_INET;
            addr.sin_port = htons(9); // discard service
            inet_pton(AF_INET, wifiAddrStr.c_str(), &addr.sin_addr);
            sendto(sock, "\0", 1, 0, (struct sockaddr *)&addr, sizeof(addr));
            err = errno;
        }
        close(sock);
    }
    else if(wifiAddrStr.find(':') > 0)
    {
        int sock = socket(AF_INET6, SOCK_DGRAM, 0);
        if (sock >= 0)
        {
            sockaddr_in6 addr = {0};
            addr.sin6_family = AF_INET6;
            addr.sin6_port = htons(9); // discard service
            inet_pton(AF_INET6, wifiAddrStr.c_str(), &addr.sin6_addr);
            sendto(sock, "\0", 1, 0, (struct sockaddr *)&addr, sizeof(addr));
            err = errno;
        }
        close(sock);
    }

    // not sure if other error code can be returned from system when the permission is not granted
    // but it seems to be a common way to implement this.
    return err != EHOSTUNREACH;
}

std::string GetDeviceOSVersion()
{
#if IS_MAC_PLATFORM || IS_CATALYST_PLATFORM // actually multiplatform
    std::string osVersion = [[[NSProcessInfo processInfo] operatingSystemVersionString] UTF8String];
#else
    std::string osVersion = [[[UIDevice currentDevice] systemVersion] UTF8String];
#endif // IS_MAC_PLATFORM

    return osVersion;
}

uint64_t GetPhysicalMemory()
{
    return [NSProcessInfo processInfo].physicalMemory;
}

std::string GetDeviceModel()
{
    utsname systemInfo;
    if (uname(&systemInfo) == 0)
    {
        // Refer to https://en.wikipedia.org/wiki/List_of_iOS_devices to translate the hardware string into a readable model name
        return std::string(systemInfo.machine);
    }
    return "Unknown";
}


int main(int argc, char** argv)
{
    fmt::println("computer name: {}", ComputerName());
    fmt::println("computer user name: {}", ComputerUserName());
    fmt::println("device model: {}", GetDeviceModel());
    fmt::println("OS language: {}", GetOSLanguage());
    fmt::println("OS locale: {}", GetOSLocale());
    fmt::println("time zone: {}", GetTimeZone());
    fmt::println("physical memory: {}", GetPhysicalMemory());
    fmt::println("device OS version: {}", GetDeviceOSVersion());
    fmt::println("system version: {}", GetSystemVersion());
    fmt::println("device UUID: {}", GetDeviceUUID());
    fmt::println("wifi address: {}", GetWifiAddress());
    fmt::println("local network permission granted: {}", IsLocalNetworkPermissionGranted());

    utsname systemInfo;
    if (uname(&systemInfo) == 0)
    {
        // Refer to https://en.wikipedia.org/wiki/List_of_iOS_devices to translate the hardware string into a readable model name
        fmt::println("machine: {}", systemInfo.machine);
        fmt::println("sysname: {}", systemInfo.sysname);
        fmt::println("nodename: {}", systemInfo.nodename);
        fmt::println("release: {}", systemInfo.release);
        fmt::println("version: {}", systemInfo.version);
    }

    return 0;
}
