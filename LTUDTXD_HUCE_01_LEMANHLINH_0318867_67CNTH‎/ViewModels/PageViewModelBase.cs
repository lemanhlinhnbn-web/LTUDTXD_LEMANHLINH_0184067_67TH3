namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.ViewModels;

public abstract class PageViewModelBase : ViewModelBase
{
    protected PageViewModelBase(string title, string subtitle)
    {
        Title = title;
        Subtitle = subtitle;
    }

    public string Title { get; }

    public string Subtitle { get; }
}
