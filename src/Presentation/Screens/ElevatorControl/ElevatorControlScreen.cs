using Application.Abstractions.Screen;
using Application.Elevators.Request;
using Domain.Common;
using Infrastructure.Persistence.SeedData;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Presentation.Extensions;
using Spectre.Console;

namespace Presentation.Screens.ElevatorControl;

/// <summary> Elevator Control Screen to request an elevator to a specific floor. </summary>
public class ElevatorControlScreen(IMediator mediator): IScreen<bool>
{
    public async Task<Result<bool>> ShowAsync(CancellationToken token)
    {
        Result<Guid>? result = null;
        var anotherRequest = true;
        while (anotherRequest)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("Elevator Control")
                    .LeftJustified()
                    .Color(Color.Blue)
            );

            var floor = AnsiConsole.Prompt(
                new TextPrompt<int>("What floor are you on?"));

            //DoTo: Direction prompt
            var buildingId = ApplicationDbContextSeedData.GetSeedBuildings().First()!.Id;
            await AnsiConsole.Status()
                .StartAsync("Requesting elevator...", async ctx =>
                {
                    var request = new RequestElevatorCommand(buildingId, floor);
                    result = await mediator.Send(request, token);
                });

            anotherRequest = result?.Match(
                onSuccess: () =>
                {
                    AnsiConsole.MarkupLine("[green]Elevator requested successfully[/]");
                    return AnsiConsole.Confirm("Do you want to request another elevator?");
                },
                onFailure: error =>
                {
                    var friendlyError = GetErrorMessage(error);
                    AnsiConsole.MarkupLine($"[red]{friendlyError}[/]");
                    return AnsiConsole.Confirm("Do you want to try again?");
                }) ?? false;
        }
        return true;
    }

    private string GetErrorMessage(Result error)
    {
        if (error.Error is ValidationError validationError)
        {
            return string.Join(", ", validationError.Errors.Select(e => e.Description));
        }
        return error.Error.Description;
    }
}
