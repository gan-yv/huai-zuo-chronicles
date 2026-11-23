using UnityEngine;

namespace Demo02.Core
{
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip ambientWind;
        [SerializeField] private AudioClip interaction;
        [SerializeField] private AudioClip confirmation;
        [SerializeField] private AudioClip menuOpen;
        [SerializeField] private AudioClip menuClose;
        [SerializeField] private AudioClip click;
        [SerializeField] private AudioClip attackSwing;
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip hurt;
        [SerializeField] private AudioClip bookOpen;
        [SerializeField] private AudioClip bookClose;
        [SerializeField] private AudioClip pageTurn;
        [SerializeField] private AudioClip[] footsteps;
        [SerializeField] private AudioClip explorationMusic;

        public AudioClip AmbientWind => ambientWind;
        public AudioClip Interaction => interaction;
        public AudioClip Confirmation => confirmation;
        public AudioClip MenuOpen => menuOpen;
        public AudioClip MenuClose => menuClose;
        public AudioClip Click => click;
        public AudioClip AttackSwing => attackSwing;
        public AudioClip Hit => hit;
        public AudioClip Hurt => hurt;
        public AudioClip BookOpen => bookOpen;
        public AudioClip BookClose => bookClose;
        public AudioClip PageTurn => pageTurn;
        public AudioClip[] Footsteps => footsteps;
        public AudioClip ExplorationMusic => explorationMusic;

        public void Configure(
            AudioClip ambientWindClip,
            AudioClip interactionClip,
            AudioClip confirmationClip,
            AudioClip menuOpenClip,
            AudioClip menuCloseClip,
            AudioClip clickClip,
            AudioClip attackSwingClip,
            AudioClip hitClip,
            AudioClip hurtClip,
            AudioClip bookOpenClip,
            AudioClip bookCloseClip,
            AudioClip pageTurnClip,
            AudioClip[] footstepClips,
            AudioClip explorationMusicClip)
        {
            ambientWind = ambientWindClip;
            interaction = interactionClip;
            confirmation = confirmationClip;
            menuOpen = menuOpenClip;
            menuClose = menuCloseClip;
            click = clickClip;
            attackSwing = attackSwingClip;
            hit = hitClip;
            hurt = hurtClip;
            bookOpen = bookOpenClip;
            bookClose = bookCloseClip;
            pageTurn = pageTurnClip;
            footsteps = footstepClips;
            explorationMusic = explorationMusicClip;
        }
    }
}
