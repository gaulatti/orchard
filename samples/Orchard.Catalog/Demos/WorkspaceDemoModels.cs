using System.Windows.Input;

namespace Orchard.Catalog.Demos;

// Catalog-only, fictional, in-memory producers. These are not Orchard APIs.
internal sealed class DemoCommand(Action execute,Func<bool>? allowed=null):ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter)=>allowed?.Invoke()??true;
    public void Execute(object? parameter){if(!CanExecute(parameter))throw new InvalidOperationException("The local demo action is unavailable.");execute();}
    internal void Refresh()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}

internal sealed record DemoPost(string Id,string Column,string Author,string Content,double Relevance,string PostedAt);
internal sealed record DemoReview(IReadOnlyList<string> PostIds,string Instructions);
internal sealed class MonitoringDemoState
{
    internal static readonly string[] Columns=["Local","Signals","Community"];
    private readonly List<DemoPost> posts=[
        new("harbor","Local","Ada Example","Harbor volunteers open a fictional reading room.",91,"2026-10-18T15:00:00Z"),
        new("garden","Local","Mira Example","A fictional community garden welcomes new readers.",74,"2026-10-18T14:50:00Z"),
        new("weather","Signals","Sam Example","A fictional weather sensor reports a clear afternoon.",86,"2026-10-18T14:40:00Z"),
        new("transit","Signals","Ada Example","A fictional transit notice adds an accessible route.",68,"2026-10-18T14:30:00Z"),
        new("school","Community","Mira Example","A fictional school hosts a neighborhood workshop.",79,"2026-10-18T14:20:00Z"),
        new("music","Community","Sam Example","A fictional music group rehearses in the harbor hall.",72,"2026-10-18T14:10:00Z")];
    private readonly Dictionary<string,string> queries=Columns.ToDictionary(column=>column,_=>"");
    private readonly HashSet<string> selected=[];
    private string[] reviewIds=[];
    internal IReadOnlyList<DemoPost> Posts=>posts;
    internal IReadOnlySet<string> SelectedIds=>selected;
    internal List<DemoReview> Reviews{get;}=[];
    internal string? InspectorId{get;private set;}
    internal string Section{get;private set;}="feeds";
    internal bool Reviewing{get;private set;}
    internal string Instructions{get;private set;}="";
    internal bool ConfirmCancel{get;private set;}
    internal IReadOnlyList<string> ReviewIds=>reviewIds;
    internal string Feedback{get;private set;}="Fictional local data. No network connection.";
    internal int UpdateCount{get;private set;}
    internal event Action? Changed;
    internal string Query(string column)=>queries[column];
    internal IReadOnlyList<DemoPost> Visible(string column)=>posts.Where(post=>post.Column==column&&(post.Content.Contains(queries[column],StringComparison.OrdinalIgnoreCase)||post.Author.Contains(queries[column],StringComparison.OrdinalIgnoreCase))).ToArray();
    internal DemoPost Post(string id)=>posts.Single(post=>post.Id==id);
    internal void Search(string column,string value){if(Reviewing)throw new InvalidOperationException("Finish the local review first.");queries[column]=value;Changed?.Invoke();}
    internal void SetSelected(string id,bool value){_=Post(id);if(Reviewing)throw new InvalidOperationException("The review selection is locked.");if(value)selected.Add(id);else selected.Remove(id);Changed?.Invoke();}
    internal void Inspect(string id){_=Post(id);InspectorId=id;Changed?.Invoke();}
    internal void SelectSection(string section){if(section is not("feeds"or"attention"or"reviewed"))throw new ArgumentOutOfRangeException(nameof(section));if(Reviewing)throw new InvalidOperationException("Finish the local review first.");Section=section;Changed?.Invoke();}
    internal void SimulateUpdate(){if(Reviewing)throw new InvalidOperationException("Finish the local review first.");UpdateCount++;posts.Insert(0,new("update-"+UpdateCount,"Local","Ada Example","Fictional harbor update "+UpdateCount+" demonstrates a live collection change.",89,"2026-10-18T15:10:00Z"));Feedback="Inserted one local update; selection and edited controls are retained.";Changed?.Invoke();}
    internal void BeginReview(){if(Reviewing||selected.Count==0)throw new InvalidOperationException("Select local posts before reviewing.");reviewIds=posts.Where(post=>selected.Contains(post.Id)).Select(post=>post.Id).ToArray();Instructions="";ConfirmCancel=false;Reviewing=true;Changed?.Invoke();}
    internal void EditInstructions(string value){if(!Reviewing)throw new InvalidOperationException("No local review is open.");Instructions=value;ConfirmCancel=false;Changed?.Invoke();}
    internal void RequestCancelReview(){if(!Reviewing)throw new InvalidOperationException("No local review is open.");if(Instructions.Length>0){ConfirmCancel=true;Changed?.Invoke();}else CloseReview();}
    internal void KeepReview(){ConfirmCancel=false;Changed?.Invoke();}
    internal void DiscardReview(){if(!ConfirmCancel)throw new InvalidOperationException("No discard decision is pending.");CloseReview();}
    internal void CommitReview(){if(!Reviewing||ConfirmCancel||reviewIds.Length==0)throw new InvalidOperationException("No local review is ready to record.");Reviews.Add(new(reviewIds.ToArray(),Instructions));foreach(var id in reviewIds)selected.Remove(id);Feedback="Recorded local review "+Reviews.Count+" for "+reviewIds.Length+" fictional posts. Nothing was sent.";CloseReview();}
    private void CloseReview(){Reviewing=false;ConfirmCancel=false;reviewIds=[];Instructions="";Changed?.Invoke();}
}

internal sealed record DemoChoice(string Id,string Label);
internal sealed class DemoPage(string id,string title,string body,string status,string language,string media,IEnumerable<string> categories,IEnumerable<string> authors)
{
    internal string Id{get;}=id;
    internal string Title{get;set;}=title;
    internal string Body{get;set;}=body;
    internal string Status{get;set;}=status;
    internal string Language{get;}=language;
    internal string MediaId{get;set;}=media;
    internal List<string> Categories{get;}=[..categories];
    internal List<string> Authors{get;}=[..authors];
}
internal sealed class DemoPageDraft(DemoPage original)
{
    internal DemoPage Original{get;}=original;
    internal string Title{get;set;}=original.Title;
    internal string Body{get;set;}=original.Body;
    internal string Status{get;set;}=original.Status;
    internal string MediaId{get;set;}=original.MediaId;
    internal List<string> Categories{get;}=[..original.Categories];
    internal List<string> Authors{get;}=[..original.Authors];
    internal bool Dirty=>Title!=Original.Title||Body!=Original.Body||Status!=Original.Status||MediaId!=Original.MediaId||!Categories.SequenceEqual(Original.Categories)||!Authors.SequenceEqual(Original.Authors);
}
internal sealed record DemoPendingAction(string Kind,string Id);
internal sealed class EditorialDemoState
{
    internal static readonly DemoChoice[] Media=[new("harbor","Fictional harbor illustration"),new("garden","Fictional garden illustration"),new("workshop","Fictional workshop illustration")];
    internal static readonly DemoChoice[] Categories=[new("local","Local"),new("culture","Culture"),new("learning","Learning")];
    internal static readonly DemoChoice[] Authors=[new("ada","Ada Example"),new("mira","Mira Example"),new("sam","Sam Example")];
    private readonly List<DemoPage> pages=[
        new("harbor-page","Harbor reading room","A fictional reading room welcomes the neighborhood.\n\nThis local paragraph demonstrates page editing.","draft","en","harbor",["local"],["ada"]),
        new("garden-page","Community garden","A fictional garden hosts a workshop.","review","en","garden",["local","learning"],["mira"]),
        new("music-page","Neighborhood music","A fictional music group shares a rehearsal.","draft","en","workshop",["culture"],["sam"]),
        new("oficina-page","Oficina da comunidade","Uma oficina fictícia demonstra conteúdo local.","review","pt","workshop",["learning"],["ada","mira"])];
    private readonly HashSet<string> selected=[];
    internal IReadOnlyList<DemoPage> Pages=>pages;
    internal IReadOnlySet<string> SelectedIds=>selected;
    internal string Query{get;private set;}="";
    internal string StatusFilter{get;private set;}="";
    internal string LanguageFilter{get;private set;}="";
    internal bool Descending{get;private set;}
    internal string Section{get;private set;}="pages";
    internal DemoPageDraft? Draft{get;private set;}
    internal DemoPendingAction? Pending{get;private set;}
    internal bool Dirty=>Draft?.Dirty==true;
    internal bool CanSave=>Draft is{} draft&&draft.Dirty&&!string.IsNullOrWhiteSpace(draft.Title)&&Pending is null;
    internal string Feedback{get;private set;}="Fictional local data. Edits reset on restart.";
    internal event Action? Changed;
    internal event Action<string>? SuiteRequested;
    internal IEnumerable<DemoPage> Visible=>Descending?Filtered.OrderByDescending(page=>page.Title,StringComparer.OrdinalIgnoreCase):Filtered.OrderBy(page=>page.Title,StringComparer.OrdinalIgnoreCase);
    private IEnumerable<DemoPage> Filtered=>pages.Where(page=>(StatusFilter==""||page.Status==StatusFilter)&&(LanguageFilter==""||page.Language==LanguageFilter)&&(page.Title.Contains(Query,StringComparison.OrdinalIgnoreCase)||page.Body.Contains(Query,StringComparison.OrdinalIgnoreCase)));
    internal DemoPage Page(string id)=>pages.Single(page=>page.Id==id);
    internal void Filter(string query,string status,string language){if(status is not(""or"draft"or"review")||language is not(""or"en"or"pt"))throw new ArgumentOutOfRangeException(nameof(status));Query=query;StatusFilter=status;LanguageFilter=language;Changed?.Invoke();}
    internal void ToggleSort(){Descending=!Descending;Changed?.Invoke();}
    internal void SetSelected(string id,bool value){_=Page(id);if(value)selected.Add(id);else selected.Remove(id);Changed?.Invoke();}
    internal void Open(string id){_=Page(id);if(Draft?.Original.Id==id)return;Navigate(new("open",id));}
    internal void SelectSection(string section){if(section is not("pages"or"media"or"categories"or"authors"))throw new ArgumentOutOfRangeException(nameof(section));if(section==Section)return;Navigate(new("section",section));}
    internal void SelectSuite(string suite){if(suite!="monitoring")throw new ArgumentOutOfRangeException(nameof(suite));Navigate(new("suite",suite));}
    private void Navigate(DemoPendingAction action){EnsureNoPending();if(Dirty){Pending=action;Changed?.Invoke();return;}Apply(action);}
    internal void CancelPending(){Pending=null;Changed?.Invoke();}
    internal void ConfirmDiscard(){var action=Pending??throw new InvalidOperationException("No discard decision is pending.");Pending=null;Draft=null;Apply(action);}
    private void Apply(DemoPendingAction action)
    {
        switch(action.Kind){case"open":Section="pages";Draft=new(Page(action.Id));break;case"section":Section=action.Id;Draft=null;break;case"suite":Draft=null;SuiteRequested?.Invoke(action.Id);break;default:throw new InvalidOperationException("Unknown local navigation action.");}Changed?.Invoke();
    }
    private void EnsureNoPending(){if(Pending is not null)throw new InvalidOperationException("Resolve the inline discard decision first.");}
    internal void Edit(string title,string body,string status){EnsureNoPending();var draft=Draft??throw new InvalidOperationException("Open a local page first.");if(status is not("draft"or"review"))throw new ArgumentOutOfRangeException(nameof(status));draft.Title=title;draft.Body=body;draft.Status=status;Changed?.Invoke();}
    internal void SelectMedia(string id){EnsureNoPending();_=Media.Single(item=>item.Id==id);(Draft??throw new InvalidOperationException("Open a local page first.")).MediaId=id;Changed?.Invoke();}
    internal void SetChoice(string kind,string id,bool chosen)
    {
        EnsureNoPending();
        var draft=Draft??throw new InvalidOperationException("Open a local page first.");var choices=kind switch{"category"=>Categories,"author"=>Authors,_=>throw new ArgumentOutOfRangeException(nameof(kind))};_=choices.Single(item=>item.Id==id);var list=kind=="category"?draft.Categories:draft.Authors;if(chosen&&!list.Contains(id))list.Add(id);else if(!chosen)list.Remove(id);Changed?.Invoke();
    }
    internal void MoveChoice(string kind,string id,int delta)
    {
        EnsureNoPending();
        var draft=Draft??throw new InvalidOperationException("Open a local page first.");var list=kind switch{"category"=>draft.Categories,"author"=>draft.Authors,_=>throw new ArgumentOutOfRangeException(nameof(kind))};var index=list.IndexOf(id);var next=index+delta;if(index<0||next<0||next>=list.Count||Math.Abs(delta)!=1)throw new ArgumentOutOfRangeException(nameof(delta));list.RemoveAt(index);list.Insert(next,id);Changed?.Invoke();
    }
    internal void Save()
    {
        if(!CanSave)throw new InvalidOperationException("The local draft cannot be saved.");var draft=Draft!;var page=draft.Original;page.Title=draft.Title;page.Body=draft.Body;page.Status=draft.Status;page.MediaId=draft.MediaId;page.Categories.Clear();page.Categories.AddRange(draft.Categories);page.Authors.Clear();page.Authors.AddRange(draft.Authors);Draft=new(page);Feedback="Saved the local sample; no external content changed.";Changed?.Invoke();
    }
}
