namespace MultiChainWallet.UI;

public partial class App : Application
{
    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    /// <param name="mainPage">主页面 / Main page</param>
    public App(MainPage mainPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(mainPage);
    }
}
