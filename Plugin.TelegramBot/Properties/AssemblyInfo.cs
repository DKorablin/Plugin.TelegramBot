using System.Reflection;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]
[assembly: Guid("680b3e5e-7ea3-4374-a83f-584bb3eced40")]
[assembly: System.CLSCompliant(true)]

#if NETCOREAPP
[assembly: AssemblyMetadata("ProjectUrl", "https://github.com/DKorablin/SAL.Interface.TelegramBot")]
#else

[assembly: AssemblyTitle("Plugin.TelegramBot")]
[assembly: AssemblyDescription("Telegram messenger bot host plugin")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("Danila Korablin")]
[assembly: AssemblyProduct("Plugin.TelegramBot")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2017-2024")]
#endif

//C:\Visual Studio Projects\C#\sal.ProcessingServices\sal.Host\Flatbed.WinService.exe
//C:\Visual Studio Projects\C#\sal.ProcessingServices\sal.Settings\Flatbed.Dialog.exe