using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchard.Catalog.Demos;

namespace Orchard.Tests;

[TestClass]
public sealed class WorkspaceDemoTests
{
    [TestMethod]
    public void MonitoringSearchAndUpdatesRetainOriginalPostsAndSelection()
    {
        var state = new MonitoringDemoState(); var post = state.Post("harbor");
        state.SetSelected(post.Id, true); state.Inspect(post.Id);
        state.Search("Local", "GARDEN");
        CollectionAssert.AreEqual(new[] { "garden" }, state.Visible("Local").Select(item => item.Id).ToArray());
        state.SimulateUpdate(); state.SimulateUpdate();
        Assert.AreSame(post, state.Post("harbor")); Assert.IsTrue(state.SelectedIds.Contains(post.Id));
        Assert.AreEqual(post.Id, state.InspectorId); Assert.AreEqual("GARDEN", state.Query("Local"));
        state.Search("Local", "");
        CollectionAssert.AreEqual(new[] { "update-2", "update-1", "harbor", "garden" }, state.Visible("Local").Select(item => item.Id).ToArray());
        Assert.IsTrue(state.Reviews.Count == 0 && state.UpdateCount == 2);
    }

    [TestMethod]
    public void MonitoringReviewLocksOriginalSelectionUntilExplicitLocalCommit()
    {
        var state = new MonitoringDemoState(); state.SetSelected("music", true); state.SetSelected("harbor", true); state.BeginReview();
        CollectionAssert.AreEqual(new[] { "harbor", "music" }, state.ReviewIds.ToArray());
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SetSelected("garden", true));
        Assert.ThrowsExactly<InvalidOperationException>(state.SimulateUpdate);
        Assert.ThrowsExactly<InvalidOperationException>(() => state.Search("Local", "garden"));
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SelectSection("reviewed"));
        state.EditInstructions("Keep this fictional summary short."); state.CommitReview();
        Assert.IsFalse(state.Reviewing); Assert.AreEqual(0, state.SelectedIds.Count);
        CollectionAssert.AreEqual(new[] { "harbor", "music" }, state.Reviews.Single().PostIds.ToArray());
        Assert.AreEqual("Keep this fictional summary short.", state.Reviews.Single().Instructions);
        Assert.IsTrue(state.Feedback.Contains("Nothing was sent", StringComparison.Ordinal));
        state.SetSelected("garden", true); state.BeginReview(); state.RequestCancelReview();
        CollectionAssert.AreEqual(new[] { "harbor", "music" }, state.Reviews.Single().PostIds.ToArray());
    }

    [TestMethod]
    public void MonitoringUnsavedReviewHasKeepAndExplicitDiscardPaths()
    {
        var state = new MonitoringDemoState(); state.SetSelected("harbor", true); state.BeginReview();
        state.EditInstructions("Unsaved instructions"); state.RequestCancelReview();
        Assert.IsTrue(state.ConfirmCancel && state.Reviewing); Assert.AreEqual(0, state.Reviews.Count);
        Assert.ThrowsExactly<InvalidOperationException>(state.CommitReview);
        state.KeepReview(); Assert.IsFalse(state.ConfirmCancel); Assert.AreEqual("Unsaved instructions", state.Instructions);
        state.RequestCancelReview(); state.DiscardReview();
        Assert.IsFalse(state.Reviewing); Assert.IsTrue(state.SelectedIds.Contains("harbor")); Assert.AreEqual(0, state.Reviews.Count);
        Assert.ThrowsExactly<InvalidOperationException>(state.DiscardReview);
    }

    [TestMethod]
    public void MonitoringInvalidActionsFailExplicitly()
    {
        var state = new MonitoringDemoState();
        Assert.ThrowsExactly<InvalidOperationException>(state.BeginReview);
        Assert.ThrowsExactly<InvalidOperationException>(state.CommitReview);
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SetSelected("missing", true));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.SelectSection("external"));
    }

    [TestMethod]
    public void EditorialFiltersAndSortingRetainOriginalRowsAndHiddenSelection()
    {
        var state = new EditorialDemoState(); var page = state.Page("harbor-page"); state.SetSelected(page.Id, true);
        state.Filter("fict", "review", "pt");
        CollectionAssert.AreEqual(new[] { "oficina-page" }, state.Visible.Select(item => item.Id).ToArray());
        Assert.IsTrue(state.SelectedIds.Contains(page.Id)); state.Filter("", "", ""); state.ToggleSort();
        CollectionAssert.AreEqual(new[] { "oficina-page", "music-page", "harbor-page", "garden-page" }, state.Visible.Select(item => item.Id).ToArray());
        Assert.AreSame(page, state.Page(page.Id)); Assert.IsTrue(state.SelectedIds.Contains(page.Id));
        state.Filter("no matches", "", ""); Assert.AreEqual(0, state.Visible.Count());
        state.Filter("", "draft", "en"); Assert.AreEqual(2, state.Visible.Count());
    }

    [TestMethod]
    public void EditorialDraftAndOrderedChoicesReachOriginalSavedPage()
    {
        var state = new EditorialDemoState(); var page = state.Page("harbor-page"); state.Open(page.Id);
        state.Edit("Revised fictional page", "Revised local body", "review"); state.SelectMedia("garden");
        state.SetChoice("category", "culture", true); state.SetChoice("category", "learning", true);
        state.MoveChoice("category", "learning", -1); state.SetChoice("category", "local", false);
        state.SetChoice("author", "mira", true); state.MoveChoice("author", "mira", -1);
        Assert.AreEqual("Harbor reading room", page.Title); Assert.IsTrue(state.CanSave);
        state.Save();
        Assert.AreSame(page, state.Page(page.Id)); Assert.AreSame(page, state.Draft!.Original);
        Assert.AreEqual("Revised fictional page", page.Title); Assert.AreEqual("Revised local body", page.Body);
        Assert.AreEqual("review", page.Status); Assert.AreEqual("garden", page.MediaId);
        CollectionAssert.AreEqual(new[] { "learning", "culture" }, page.Categories);
        CollectionAssert.AreEqual(new[] { "mira", "ada" }, page.Authors);
        Assert.IsFalse(state.Dirty); Assert.IsFalse(state.CanSave);
    }

    [TestMethod]
    public void EditorialRevertingEveryFieldRestoresCleanState()
    {
        var state = new EditorialDemoState(); state.Open("harbor-page"); var original = state.Draft!.Original;
        state.Edit("Other", "Other body", "review"); state.SelectMedia("garden");
        state.SetChoice("author", "sam", true); state.SetChoice("category", "culture", true);
        Assert.IsTrue(state.Dirty);
        state.Edit(original.Title, original.Body, original.Status); state.SelectMedia(original.MediaId);
        state.SetChoice("author", "sam", false); state.SetChoice("category", "culture", false);
        Assert.IsFalse(state.Dirty); Assert.IsFalse(state.CanSave);
    }

    [TestMethod]
    public void EditorialRowNavigationRequiresDiscardAndKeepRetainsExactDraft()
    {
        var state = new EditorialDemoState(); state.Open("harbor-page"); state.Edit("Unsaved", "Unsaved body", "review"); var draft = state.Draft;
        state.Open("garden-page"); Assert.AreEqual("open", state.Pending!.Kind); Assert.AreSame(draft, state.Draft); Assert.IsFalse(state.CanSave);
        Assert.ThrowsExactly<InvalidOperationException>(() => state.Edit("Changed during decision", "Body", "draft"));
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SelectMedia("garden"));
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SetChoice("category", "culture", true));
        Assert.ThrowsExactly<InvalidOperationException>(() => state.MoveChoice("author", "ada", 1));
        state.CancelPending(); Assert.AreSame(draft, state.Draft); Assert.AreEqual("Unsaved", state.Draft!.Title);
        state.Open("garden-page"); state.ConfirmDiscard(); Assert.AreEqual("garden-page", state.Draft!.Original.Id);
        Assert.AreEqual("Harbor reading room", state.Page("harbor-page").Title); Assert.IsNull(state.Pending);
    }

    [TestMethod]
    public void EditorialSectionAndSuiteNavigationWaitForExplicitDiscard()
    {
        var state = new EditorialDemoState(); string? suite = null; state.SuiteRequested += value => suite = value;
        state.Open("harbor-page"); state.Edit("Unsaved", "Body", "draft"); state.SelectSection("media");
        Assert.AreEqual("pages", state.Section); Assert.AreEqual("section", state.Pending!.Kind); state.CancelPending();
        state.SelectSuite("monitoring"); Assert.IsNull(suite); Assert.AreEqual("suite", state.Pending!.Kind); state.CancelPending();
        state.SelectSuite("monitoring"); state.ConfirmDiscard(); Assert.AreEqual("monitoring", suite); Assert.IsNull(state.Draft);
        state.Open("harbor-page"); state.Edit("Unsaved", "Body", "draft"); state.SelectSection("authors"); state.ConfirmDiscard();
        Assert.AreEqual("authors", state.Section); Assert.IsNull(state.Draft);
    }

    [TestMethod]
    public void EditorialValidationPreventsInvalidLocalSaveAndRelations()
    {
        var state = new EditorialDemoState(); Assert.ThrowsExactly<InvalidOperationException>(state.Save);
        state.Open("harbor-page"); state.Edit(" ", "Body", "draft"); Assert.IsFalse(state.CanSave); Assert.ThrowsExactly<InvalidOperationException>(state.Save);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.Edit("Title", "Body", "published"));
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SelectMedia("missing"));
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SetChoice("author", "missing", true));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.MoveChoice("author", "ada", -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.Filter("", "published", "en"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.SelectSuite("external"));
        Assert.ThrowsExactly<InvalidOperationException>(state.ConfirmDiscard);
    }

    [TestMethod]
    public void CommandsRejectUnavailableActionsAndNotifyNativeConsumers()
    {
        var allowed = false; var executions = 0; var changes = 0;
        var command = new DemoCommand(() => executions++, () => allowed); command.CanExecuteChanged += (_, _) => changes++;
        Assert.IsFalse(command.CanExecute(null)); Assert.ThrowsExactly<InvalidOperationException>(() => command.Execute(null));
        allowed = true; command.Refresh(); command.Execute(null);
        Assert.AreEqual(1, changes); Assert.AreEqual(1, executions);
    }
}
