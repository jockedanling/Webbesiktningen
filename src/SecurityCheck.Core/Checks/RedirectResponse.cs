namespace SecurityCheck.Core.Checks;

/// <summary>
/// Det HttpsRedirectCheck behöver veta om svaret på http://domän/. En enkel record istället för
/// HttpResponseMessage som måste dispose:as.
/// </summary>
public sealed record RedirectResponse(
    int StatusCode,
    Uri? Location); // null om svaret inte har någon Location-header
