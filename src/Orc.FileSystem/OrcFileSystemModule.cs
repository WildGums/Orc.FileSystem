namespace Orc
{
    using Catel.Services;
    using Catel.ThirdPartyNotices;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Orc.FileSystem;

    /// <summary>
    /// Core module which allows the registration of default services in the service collection.
    /// </summary>
    public static class OrcFileSystemModule
    {
        public static IServiceCollection AddOrcFileSystem(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IFileService, FileService>();
            serviceCollection.TryAddSingleton<IDirectoryService, DirectoryService>();
            serviceCollection.TryAddSingleton<IIOSynchronizationService, IOSynchronizationService>();

            serviceCollection.AddSingleton<IThirdPartyNotice>((x) => new LibraryThirdPartyNotice("Orc.FileSystem", "https://github.com/wildgums/orc.filesystem"));

            return serviceCollection;
        }
    }
}
