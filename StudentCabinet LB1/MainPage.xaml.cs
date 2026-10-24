using StudentCabinet_LB1.ViewModels;

namespace StudentCabinet_LB1
{
    public partial class MainPage : ContentPage
    {
        private readonly StudentViewModel _viewModel;

        // ViewModel підставляє DI-контейнер
        public MainPage(StudentViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadCommand.Execute(null);   // завантаження даних через ViewModel
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.OnPageDisappearing();        // логування переходу
        }
    }
}