using System;
using CodeHub.Core.ViewModels.Source;
using UIKit;
using ReactiveUI;
using CodeHub.iOS.Cells;
using System.Reactive.Linq;

namespace CodeHub.iOS.Views.Source
{
    public class BranchesAndTagsView : BaseTableViewController<BranchesAndTagsViewModel>
	{
        private readonly UISegmentedControl _viewSegment = new UISegmentedControl(new object[] {"Branches", "Tags"});

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

            NavigationItem.TitleView = _viewSegment;

            _viewSegment.ValueChanged += (sender, args) => ViewModel.SelectedFilter = (BranchesAndTagsViewModel.ShowIndex) (int)_viewSegment.SelectedSegment;
            ViewModel.WhenAnyValue(x => x.SelectedFilter).Subscribe(x => _viewSegment.SelectedSegment = (int)x);

            TableView.RegisterClassForCellReuse(typeof(BranchCellView), BranchCellView.Key);
            TableView.RegisterClassForCellReuse(typeof(TagCellView), TagCellView.Key);
 
            ViewModel.WhenAnyValue(x => x.SelectedFilter)
                .Subscribe(x =>
                {
                    if (TableView.Source != null)
                    {
                        TableView.Source.Dispose();
                        TableView.Source = null;
                    }

                    if (x == BranchesAndTagsViewModel.ShowIndex.Branches)
                    {
                        var source = new ReactiveTableViewSource<BranchItemViewModel>(TableView, ViewModel.Branches, BranchCellView.Key, 44f);
                        source.ElementSelected.OfType<BranchItemViewModel>().Subscribe(y => y.GoToCommand.ExecuteIfCan());
                        TableView.Source = source;
                    }
                    else
                    {
                        var source = new ReactiveTableViewSource<TagItemViewModel>(TableView, ViewModel.Tags, TagCellView.Key, 44f);
                        source.ElementSelected.OfType<TagItemViewModel>().Subscribe(y => y.GoToCommand.ExecuteIfCan());
                        TableView.Source = source;
                    }
                });
		}
	}
}

