﻿using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("680b3e5e-7ea3-4374-a83f-584bb3eced40")]
[assembly: System.CLSCompliant(true)]

#if NETSTANDARD || NETCOREAPP
[assembly: AssemblyMetadata("ProjectUrl", "https://github.com/DKorablin/SAL.Interface.TelegramBot")]
#else

[assembly: AssemblyDescription("Telegram messenger bot host plugin")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2017-2025")]
#endif

//C:\Visual Studio Projects\C#\sal.ProcessingServices\sal.Host\Flatbed.WinService.exe
//C:\Visual Studio Projects\C#\sal.ProcessingServices\sal.Settings\Flatbed.Dialog.exe