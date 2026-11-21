namespace Demo02.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        string Interact(PlayerInteractor interactor);
    }
}
