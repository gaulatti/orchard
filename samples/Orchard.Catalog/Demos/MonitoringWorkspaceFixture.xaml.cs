using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
namespace Orchard.Catalog.Demos;
public sealed partial class MonitoringWorkspaceFixture:UserControl
{
    internal MonitoringDemoState Model{get;}=new();
    private readonly Dictionary<string,PostView> posts=[];
    private readonly Dictionary<string,(BleeckerFeedColumn Column,StackPanel Body,BleeckerSearchInput Search)> columns=[];
    private bool updating,reviewWasOpen;
    public event Action<string>? SuiteRequested;
    public MonitoringWorkspaceFixture()
    {
        InitializeComponent();Suite.Options=[new("monitoring","Monitoring demo"),new("editorial","Editorial demo")];Suite.ValueRequested+=(_,args)=>{if(!Model.Reviewing)SuiteRequested?.Invoke(args.Value);};
        foreach(var(id,label)in new[]{("feeds","Live columns"),("attention","Attention cards"),("reviewed","Local reviews")})Sidebar.Items.Add(new(){Id=id,Label=label,Href="#demo-"+id,Active=id=="feeds",Command=new DemoCommand(()=>Model.SelectSection(id),()=>!Model.Reviewing)});
        foreach(var key in MonitoringDemoState.Columns)
        {
            var body=new StackPanel{Spacing=12};var search=new BleeckerSearchInput{PlaceholderText="Search "+key,InputSize=BleeckerInputSize.Small};WorkspaceDemoUi.Name(search,"Search fictional "+key+" posts");
            search.RegisterPropertyChangedCallback(BleeckerSearchInput.TextProperty,(_,_)=>{if(!updating)Model.Search(key,search.Text);});
            var column=new BleeckerFeedColumn{Title=key,Height=420,Content=body,Filter=search};columns.Add(key,(column,body,search));Columns.Children.Add(column);
        }
        Update.Command=new DemoCommand(Model.SimulateUpdate,()=>!Model.Reviewing);Review.Command=new DemoCommand(Model.BeginReview,()=>!Model.Reviewing&&Model.SelectedIds.Count>0);Back.Command=new DemoCommand(Model.RequestCancelReview);RecordReview.Command=new DemoCommand(Model.CommitReview,()=>Model.Reviewing&&!Model.ConfirmCancel);KeepReview.Command=new DemoCommand(Model.KeepReview);ConfirmDiscard.Command=new DemoCommand(Model.DiscardReview);
        Instructions.RegisterPropertyChangedCallback(BleeckerTextarea.TextProperty,(_,_)=>{if(!updating&&Model.Reviewing)Model.EditInstructions(Instructions.Text);});Model.Changed+=Render;Loaded+=(_,_)=>Render();Render();
    }
    private void Render()
    {
        if(updating)return;updating=true;
        try
        {
            foreach(var post in Model.Posts)if(!posts.ContainsKey(post.Id))posts.Add(post.Id,new PostView(this,post));
            foreach(var(key,entry)in columns)
            {
                var visible=Model.Visible(key).Where(post=>Model.Section!="attention"||post.Relevance>=80).ToArray();entry.Column.Count=visible.Length;WorkspaceDemoUi.Reconcile(entry.Body,visible.Select(post=>(UIElement)posts[post.Id].Surface).ToArray());
                foreach(var post in visible)posts[post.Id].Selection.IsChecked=Model.SelectedIds.Contains(post.Id);
                if(entry.Search.Text!=Model.Query(key))entry.Search.Text=Model.Query(key);
            }
            foreach(var item in Sidebar.Items){item.Active=item.Id==Model.Section;((DemoCommand)item.Command!).Refresh();}
            FeedScroll.Visibility=Model.Section=="reviewed"?Visibility.Collapsed:Visibility.Visible;HistoryScroll.Visibility=Model.Section=="reviewed"?Visibility.Visible:Visibility.Collapsed;
            if(Model.Section=="reviewed"){History.Children.Clear();if(Model.Reviews.Count==0)History.Children.Add(WorkspaceDemoUi.Text("No local reviews recorded. Select posts and review them first."));foreach(var review in Model.Reviews)History.Children.Add(WorkspaceDemoUi.Text(string.Join(" · ",review.PostIds.Select(id=>Model.Post(id).Content))+"\n"+review.Instructions));}
            Summary.Text=$"{Model.SelectedIds.Count} selected · {Model.UpdateCount} local updates · {Model.Reviews.Count} local reviews";Feedback.Text=Model.Feedback;
            Inspector.Visibility=Model.InspectorId is null||Model.Section=="reviewed"?Visibility.Collapsed:Visibility.Visible;if(Model.InspectorId is{} id){var post=Model.Post(id);InspectorText.Text=post.Author+"\n"+post.Content;}
            Sidebar.IsEnabled=!Model.Reviewing;foreach(var entry in columns.Values)entry.Column.IsEnabled=!Model.Reviewing;Inspector.IsEnabled=!Model.Reviewing;Suite.IsEnabled=!Model.Reviewing;ReviewOverlay.Visibility=Model.Reviewing?Visibility.Visible:Visibility.Collapsed;
            if(Instructions.Text!=Model.Instructions)Instructions.Text=Model.Instructions;
            ReviewSummary.Text=Model.Reviewing?string.Join("\n",Model.ReviewIds.Select(id=>Model.Post(id).Content))+"\nThis records a local example only. Nothing is sent.":"";
            DiscardReview.Visibility=Model.ConfirmCancel?Visibility.Visible:Visibility.Collapsed;Instructions.IsEnabled=!Model.ConfirmCancel;Back.IsEnabled=!Model.ConfirmCancel;
            foreach(var button in new[]{Update,Review,Back,RecordReview,KeepReview,ConfirmDiscard})((DemoCommand)button.Command!).Refresh();
            if(XamlRoot?.IsHostVisible==true&&reviewWasOpen!=Model.Reviewing){if(Model.Reviewing){ReviewOverlay.UpdateLayout();Instructions.Focus(FocusState.Programmatic);}else Review.Focus(FocusState.Programmatic);}reviewWasOpen=Model.Reviewing;
        }
        finally{updating=false;}
    }
    private void ReviewKeyDown(object sender,KeyRoutedEventArgs args){if(args.Key==Windows.System.VirtualKey.Escape){Model.RequestCancelReview();args.Handled=true;}}
    internal BleeckerCheckbox Selection(string id)=>posts[id].Selection;
    internal BleeckerButton Details(string id)=>posts[id].Details;
    internal (BleeckerAttentionSurface Surface, Border Card, BleeckerFeedItem Feed) PostPresentation(string id)=>
        (posts[id].Surface,posts[id].ContentCard,posts[id].Feed);
    internal BleeckerSearchInput Search(string key)=>columns[key].Search;
    private sealed class PostView
    {
        internal BleeckerAttentionSurface Surface{get;}
        internal Border ContentCard{get;}
        internal BleeckerFeedItem Feed{get;}
        internal BleeckerCheckbox Selection{get;}=new(){Content="Select"};
        internal BleeckerButton Details{get;}
        internal PostView(MonitoringWorkspaceFixture owner,DemoPost post)
        {
            var content=new StackPanel{Spacing=8};var bar=new StackPanel{Orientation=Orientation.Horizontal,Spacing=12};WorkspaceDemoUi.Name(Selection,"Select fictional post: "+post.Content);
            Selection.Checked+=(_,_)=>{if(!owner.updating)owner.Model.SetSelected(post.Id,true);};Selection.Unchecked+=(_,_)=>{if(!owner.updating)owner.Model.SetSelected(post.Id,false);};Details=WorkspaceDemoUi.Button("Details",()=>owner.Model.Inspect(post.Id),BleeckerButtonVariant.Ghost);WorkspaceDemoUi.Name(Details,"Inspect fictional post: "+post.Content);bar.Children.Add(Selection);bar.Children.Add(Details);content.Children.Add(bar);
            Feed=new BleeckerFeedItem{Author=post.Author,Content=post.Content,PostedAt=post.PostedAt,Relevance=post.Relevance,Categories=[post.Column]};content.Children.Add(Feed);
            ContentCard=new Border{Style=(Style)owner.Resources["MonitoringPostContentStyle"],Child=content};
            Surface=new BleeckerAttentionSurface{Hue=post.Column switch{"Local"=>210,"Signals"=>35,_=>145},Intensity=post.Relevance/10,Content=ContentCard};
        }
    }
}
