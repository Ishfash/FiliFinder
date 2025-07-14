// EyedropperInterop.cs
using System.Threading.Tasks;
using Microsoft.JSInterop;

public class EyedropperInterop
{
    private readonly IJSRuntime _jsRuntime;

    public EyedropperInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> OpenEyeDropperAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("window.initializeEyeDropper");
        }
        catch (JSException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}