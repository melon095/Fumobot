using Autofac;
using Fumo.Application.Web.Service;
using Fumo.Shared.Repositories;

namespace Fumo.Application.Startable;

internal class ChainStarter(
        BackgroundJobStarter BackgroundJob,
        CreateBotMetadataStarter CreateBotMetadata,
        IrcStarter IrcStarter,
        DescriptionService DescriptionService,
        CommandRepository CommandRepository,
        CancellationToken ct) : IStartable
{
    public void Start()
    {
        // TODO: This should be done without IStartable.
        //       "OnActivated" might work. BUt the starters need to be chained.

        CommandRepository.LoadAssemblyCommands();

        Task.Run(async () =>
        {
            await BackgroundJob.RegisterJobs();
            await CreateBotMetadata.Start();
            await IrcStarter.Start();
            await DescriptionService.Prepare(ct);
        }).Wait();
    }
}
