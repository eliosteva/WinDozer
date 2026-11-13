using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinDozer.Pages
{
	public sealed partial class AboutPage : Page
	{
		public AboutPage()
		{
			InitializeComponent();

			// in the page constructor, after InitializeComponent()
			VersionText.Text = "Version " + typeof(App).Assembly.GetName().Version?.ToString(3);
		}
	}
}
