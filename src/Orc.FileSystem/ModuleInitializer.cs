﻿using Catel.IoC;
using Catel.Services;
using Orc.FileSystem;

/// <summary>
/// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
/// </summary>
public static class ModuleInitializer
{
    /// <summary>
    /// Initializes the module.
    /// </summary>
    public static void Initialize()
    {
        var serviceLocator = ServiceLocator.Default;

        serviceLocator.RegisterType<IFileService, FileService>();
        serviceLocator.RegisterType<IDirectoryService, DirectoryService>();
        serviceLocator.RegisterType<IIOSynchronizationService, IOSynchronizationService>();

        var languageService = serviceLocator.ResolveRequiredType<ILanguageService>();
        languageService.RegisterLanguageSource(new LanguageResourceSource("Orc.FileSystem", "Orc.FileSystem.Properties", "Resources"));
    }
}
