namespace Cthangover.Mods.Avatars
{
    /// <summary>Public parameterless implementations are discovered and removed with their owning mod.</summary>
    public interface IAvatarRendererFactory
    {
        /// <summary>Renderer identifier used in avatar definitions, for example layers.</summary>
        string Id { get; }
        /// <summary>Creates an owned renderer; the caller must dispose it even if applying a target fails.</summary>
        IAvatarRenderer Create();
    }
}
