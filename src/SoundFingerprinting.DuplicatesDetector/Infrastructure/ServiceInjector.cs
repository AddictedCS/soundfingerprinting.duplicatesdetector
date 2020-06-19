namespace SoundFingerprinting.DuplicatesDetector.Infrastructure
{
    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.Bass;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.DuplicatesDetector.Services;
    using SoundFingerprinting.InMemory;

    public static class ServiceInjector
    {
        public static void InjectServices()
        {
            ServiceContainer.Kernel.Bind<IFolderBrowserDialogService>().To<FolderBrowserDialogService>();
            ServiceContainer.Kernel.Bind<IMessageBoxService>().To<MessageBoxService>();
            ServiceContainer.Kernel.Bind<IOpenFileDialogService>().To<OpenFileDialogService>();
            ServiceContainer.Kernel.Bind<ISaveFileDialogService>().To<SaveFileDialogService>();
            ServiceContainer.Kernel.Bind<IWindowService>().To<WindowService>();
            ServiceContainer.Kernel.Bind<IGenericViewWindow>().To<GenericViewWindowService>();

            ServiceContainer.Kernel.Bind<IFingerprintCommandBuilder>().ToConstant(FingerprintCommandBuilder.Instance).InSingletonScope();
            ServiceContainer.Kernel.Bind<IQueryFingerprintService>().ToConstant(QueryFingerprintService.Instance).InSingletonScope();
            ServiceContainer.Kernel.Bind<DuplicatesDetectorFacade>().ToSelf().InSingletonScope();
            ServiceContainer.Kernel.Bind<TrackHelper>().ToSelf().InSingletonScope();
            ServiceContainer.Kernel.Bind<IAudioService>().To<BassAudioService>().InSingletonScope();
            ServiceContainer.Kernel.Bind<IPlayAudioFileService>().To<BassPlayAudioFileService>().InSingletonScope();
            ServiceContainer.Kernel.Bind<IAdvancedModelService>().To<InMemoryModelService>().InSingletonScope();
        }
    }
}