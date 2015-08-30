using System;
using ReactiveUI;
using UIKit;
using System.Reactive.Linq;
using CodeHub.Core.ViewModels;
using CoreGraphics;
using System.Reactive.Subjects;
using CodeHub.iOS.TableViewSources;
using Splat;
using CodeHub.Core.Services;
using CodeHub.iOS.ViewControllers;
using CodeHub.iOS.Views;
using System.Collections.Generic;

namespace CodeHub.iOS.ViewControllers
{
    public abstract class BaseTableViewController<TViewModel> : BaseTableViewController, IViewFor<TViewModel> where TViewModel : class
    {
        private readonly Lazy<LoadingIndicatorView> _loadingActivityView = new Lazy<LoadingIndicatorView>(() => new LoadingIndicatorView());

        private TViewModel _viewModel;
        public TViewModel ViewModel
        {
            get { return _viewModel; }
            set { this.RaiseAndSetIfChanged(ref _viewModel, value); }
        }

        public Lazy<UIView> EmptyView { get; set; }

        object IViewFor.ViewModel
        {
            get { return _viewModel; }
            set { ViewModel = (TViewModel)value; }
        }

        protected BaseTableViewController()
            : this(UITableViewStyle.Plain)
        {
        }

        protected BaseTableViewController(UITableViewStyle style)
            : base(style)
        {
            this.WhenAnyValue(x => x.ViewModel)
                .OfType<IProvidesTitle>()
                .Select(x => x.WhenAnyValue(y => y.Title))
                .Switch().Subscribe(x => Title = x ?? string.Empty);

            this.WhenAnyValue(x => x.ViewModel)
                .OfType<IRoutingViewModel>()
                .Select(x => x.RequestNavigation)
                .Switch()
                .Subscribe(x => {
                    var viewModelViewService = Locator.Current.GetService<IViewModelViewService>();
                    var serviceConstructor = Locator.Current.GetService<IServiceConstructor>();
                    var viewType = viewModelViewService.GetViewFor(x.GetType());
                    var view = (IViewFor)serviceConstructor.Construct(viewType);
                    view.ViewModel = x;
                    HandleNavigation(x, view as UIViewController);
                });

            this.Appearing
                .Take(1)
                .Subscribe(_ => SetupLoadMore());

            this.Appeared.Take(1)
                .Select(_ => this.WhenAnyValue(x => x.ViewModel))
                .Switch()
                .OfType<IProvidesEmpty>()
                .Select(x => x.WhenAnyValue(y => y.IsEmpty))
                .Switch()
                .Subscribe(CreateEmptyHandler);
        }

        protected virtual void HandleNavigation(IBaseViewModel viewModel, UIViewController view)
        {
            if (view is IModalViewController)
            {
                PresentViewController(new ThemedNavigationController(view), true, null);
                viewModel.RequestDismiss.Subscribe(_ => DismissViewController(true, null));
            }
            else
            {
                NavigationController.PushViewController(view, true);
                viewModel.RequestDismiss.Subscribe(_ => NavigationController.PopToViewController(this, true));
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            CreateSearchBar();
            LoadViewModel();
        }

        private void SetupLoadMore()
        {
            var iPaginatableViewModel = ViewModel as IPaginatableViewModel;
            var iSourceInformsEnd = TableView.Source as IInformsEnd;

            if (iPaginatableViewModel != null && iSourceInformsEnd != null)
            {
                iSourceInformsEnd.RequestMore.Select(__ => iPaginatableViewModel.LoadMoreCommand).IsNotNull().Subscribe(async x => {
                    _loadingActivityView.Value.StartAnimating();
                    TableView.TableFooterView = _loadingActivityView.Value;

                    await x.ExecuteAsync();

                    TableView.TableFooterView = null;
                    _loadingActivityView.Value.StopAnimating();
                });
            }
        }

        protected virtual void LoadViewModel()
        {
            var iLoadableViewModel = ViewModel as ILoadableViewModel;

            if (iLoadableViewModel == null)
                return;

            var refreshControl = new UIRefreshControl();
            refreshControl.ValueChanged += async (sender, e) => {
                if (iLoadableViewModel.LoadCommand.CanExecute(null))
                {
                    await iLoadableViewModel.LoadCommand.ExecuteAsync();
                    refreshControl.EndRefreshing();
                    if (EmptyView.IsValueCreated && EmptyView.Value.Superview != null)
                        TableView.BringSubviewToFront(EmptyView.Value);
                }
            };

            iLoadableViewModel.LoadCommand.IsExecuting
                .Where(x => x && !refreshControl.Refreshing).Subscribe(_ =>
                    {
                        nint rows = 0;
                        if (TableView.Source != null)
                        {
                            for (var i = 0; i < TableView.Source.NumberOfSections(TableView); i++)
                                rows += TableView.Source.RowsInSection(TableView, i);
                        }

                        if (rows == 0)
                        {
                            _loadingActivityView.Value.StartAnimating();
                            TableView.TableFooterView = _loadingActivityView.Value;
                            RefreshControl.Do(x => x.EndRefreshing());
                            RefreshControl = null;
                        }
                    });

            iLoadableViewModel.LoadCommand.IsExecuting
                .Where(x => !x).Subscribe(_ =>
                    {
                        _loadingActivityView.Value.StopAnimating();
                        if (TableView.TableFooterView != null)
                        {
                            TableView.TableFooterView = null;
                            TableView.ReloadData();
                        }

                        if (RefreshControl == null)
                            RefreshControl = refreshControl;
                    });
        }

        private void CreateEmptyHandler(bool x)
        {
            if (x)
            {
                if (!EmptyView.IsValueCreated)
                {
                    EmptyView.Value.Alpha = 0f;
                    TableView.AddSubview(EmptyView.Value);
                }

                EmptyView.Value.UserInteractionEnabled = true;
                EmptyView.Value.Frame = new CGRect(0, 0, TableView.Bounds.Width, TableView.Bounds.Height * 2f);
                TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
                TableView.BringSubviewToFront(EmptyView.Value);
                TableView.TableHeaderView.Do(y => y.Hidden = true);
                UIView.Animate(0.2f, 0f, UIViewAnimationOptions.AllowUserInteraction | UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.BeginFromCurrentState,
                    () => EmptyView.Value.Alpha = 1.0f, null);
            }
            else if (EmptyView.IsValueCreated)
            {
                EmptyView.Value.UserInteractionEnabled = false;
                TableView.TableHeaderView.Do(y => y.Hidden = false);
                TableView.SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine;
                UIView.Animate(0.1f, 0f, UIViewAnimationOptions.AllowUserInteraction | UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.BeginFromCurrentState,
                    () => EmptyView.Value.Alpha = 0f, null);
            }
        }


        protected virtual void CreateSearchBar()
        {
            var searchableViewModel = ViewModel as IProvidesSearchKeyword;
            if (searchableViewModel != null)
                this.AddSearchBar(x => searchableViewModel.SearchKeyword = x);
        }
    }

    public class BaseTableViewController : ReactiveTableViewController, IActivatable
    {
        private readonly ISubject<bool> _appearingSubject = new Subject<bool>();
        private readonly ISubject<bool> _appearedSubject = new Subject<bool>();
        private readonly ISubject<bool> _disappearingSubject = new Subject<bool>();
        private readonly ISubject<bool> _disappearedSubject = new Subject<bool>();
        private readonly ICollection<IDisposable> _activations = new LinkedList<IDisposable>();

        public IObservable<bool> Appearing
        {
            get { return _appearingSubject; }
        }

        public IObservable<bool> Appeared
        {
            get { return _appearedSubject; }
        }

        public IObservable<bool> Disappearing
        {
            get { return _disappearingSubject; }
        }

        public IObservable<bool> Disappeared
        {
            get { return _disappearedSubject; }
        }

        public void OnActivation(Action<Action<IDisposable>> d)
        {
            Appearing.Take(1).Subscribe(_ => d(x => _activations.Add(x)));
        }

        public BaseTableViewController(UITableViewStyle style)
            : base(style)
        {
            NavigationItem.BackBarButtonItem = new UIBarButtonItem { Title = string.Empty };
            ClearsSelectionOnViewWillAppear = false;
            Appeared.Take(1).Subscribe(_ => this.TrackScreen());
            Disappeared.Subscribe(_ => {
                    foreach (var a in _activations)
                        a.Dispose();
                    _activations.Clear();
                });
            this.WhenActivated(_ => { });
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            TableView.TintColor = Theme.PrimaryNavigationBarColor;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            _appearingSubject.OnNext(animated);
            TableView.DeselectRow(TableView.IndexPathForSelectedRow, animated);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            _appearedSubject.OnNext(animated);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            _disappearingSubject.OnNext(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            _disappearedSubject.OnNext(animated);
        }
    }
}

