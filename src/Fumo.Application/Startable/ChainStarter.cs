using Autofac;

namespace Fumo.Application.Startable;

internal class ChainStarter : IStartable
{
    private readonly BackgroundJobStarter BackgroundJob;
    private readonly CreateBotMetadataStarter CreateBotMetadata;
    private readonly IrcStarter IrcStarter;

    public ChainStarter(BackgroundJobStarter backgroundJob, CreateBotMetadataStarter createBotMetadata, IrcStarter ircStarter)
    {
        BackgroundJob = backgroundJob;
        CreateBotMetadata = createBotMetadata;
        IrcStarter = ircStarter;
    }

    public void Start()
    {
        // TODO: This should be done without IStartable.
        //       "OnActivated" might work. BUt the starters need to be chained.

        Task.Run(async () =>
        {
            await BackgroundJob.RegisterJobs();
            await CreateBotMetadata.Start();
            await IrcStarter.Start();
        });
    }
}
