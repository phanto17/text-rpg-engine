namespace RpgEngine.Pixel.Presentation;

/// <summary>Keeps a caption visible until its room beat finishes, then holds and fades it.</summary>
public sealed class CaptionLifetime
{
    private const double FadeInSeconds = 0.5;
    private const double HoldSeconds = 2;
    private const double FadeOutSeconds = 1.2;
    private string? _roomId;
    private double _age;
    private double _afterReveal;
    private bool _revealed;

    public string? Text { get; private set; }

    public double Opacity => Text is null ? 0 : Math.Clamp(Math.Min(_age / FadeInSeconds,
        _revealed ? (HoldSeconds + FadeOutSeconds - _afterReveal) / FadeOutSeconds : 1), 0, 1);

    public void Show(string roomId, string? text)
    {
        _roomId = roomId;
        Text = string.IsNullOrWhiteSpace(text) ? null : text;
        _age = 0;
        _afterReveal = 0;
        _revealed = false;
    }

    public void Complete(string roomId)
    {
        if (_roomId == roomId)
        {
            _revealed = true;
        }
    }

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || Text is null)
        {
            return;
        }

        _age += seconds;
        if (_revealed)
        {
            _afterReveal += seconds;
            if (_afterReveal >= HoldSeconds + FadeOutSeconds)
            {
                Text = null;
            }
        }
    }
}
