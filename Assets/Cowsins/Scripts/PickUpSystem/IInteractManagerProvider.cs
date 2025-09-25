namespace cowsins
{
    // Implemented by InteractManager and required by PlayerDependencies
    public interface IInteractManagerProvider
    {
        float ProgressElapsed { get; }
        bool Inspecting { get; }
        Interactable HighlightedInteractable { get; }
    }
}