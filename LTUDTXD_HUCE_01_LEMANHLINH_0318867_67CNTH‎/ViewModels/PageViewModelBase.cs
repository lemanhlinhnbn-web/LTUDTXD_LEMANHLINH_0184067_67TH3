namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.ViewModels;

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
