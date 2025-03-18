using Cellm.AddIn.Exceptions;
using MediatR;
using Microsoft.Playwright;

namespace Cellm.Tools.WebReader;

internal class WebReaderRequestHandler : IRequestHandler<WebReaderRequest, WebReaderResponse>
{
    public async Task<WebReaderResponse> Handle(WebReaderRequest request, CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Firefox.LaunchAsync();
        var page = await browser.NewPageAsync();
        await page.GotoAsync(request.URL);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var text = await page.EvaluateAsync("document.body.innerText") ?? throw new CellmException("Failed to read web page");

        return new WebReaderResponse(text.ToString());
    }
}
