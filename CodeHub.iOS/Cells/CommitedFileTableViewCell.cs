using Foundation;
using UIKit;
using ReactiveUI;
using CodeHub.Core.ViewModels.Source;

namespace CodeHub.iOS.Cells
{
    public class CommitedFileTableViewCell : ReactiveTableViewCell<CommitedFileItemViewModel>
    {
        public static NSString Key = new NSString("commitedfile");

        [Export("initWithStyle:reuseIdentifier:")]
        public CommitedFileTableViewCell(UITableViewCellStyle style, NSString reuseIdentifier)
            : base(UITableViewCellStyle.Subtitle, reuseIdentifier)
        {
            ImageView.Image = Octicon.FileCode.ToImage();
            DetailTextLabel.TextColor = Theme.MainSubtitleColor;

            this.WhenActivated(d => {
                d(this.OneWayBind(ViewModel, x => x.Name, x => x.TextLabel.Text));
                d(this.OneWayBind(ViewModel, x => x.Subtitle, x => x.DetailTextLabel.Text));
            });
        }
    }
}

