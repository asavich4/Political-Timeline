using UnityEngine;

namespace PoliticalTimeline
{
    [DisallowMultipleComponent]
    public sealed class PartyAudio : MonoBehaviour
    {
        [Header("Replace these clips with any imported music or card sounds")]
        public AudioClip backgroundMusic;
        public AudioClip[] swipeClips;
        public AudioClip acknowledgeClip;
        [Header("Mix")]
        [Range(0,1)] public float musicVolume=.32f;
        [Range(0,1)] public float cardVolume=.65f;
        [Min(.01f)] public float fadeSeconds=2f;
        public bool muteMusic, muteCards;
        public AudioSource musicSource, cardSource;
        int nextSwipe;
        float fade;
        bool suspended, started;

        void Start()
        {
            started=true;
            BeginMusic();
        }
        void BeginMusic()
        {
            if(musicSource==null || backgroundMusic==null) return;
            musicSource.clip=backgroundMusic; musicSource.loop=true;
            musicSource.volume=0; musicSource.Play();
        }
        void Update()
        {
            if(suspended || musicSource==null) return;
            fade=Mathf.MoveTowards(fade,1,Time.unscaledDeltaTime/Mathf.Max(.01f,fadeSeconds));
            musicSource.volume=muteMusic?0:musicVolume*fade;
        }
        // Called only once a card commits, including keyboard choices and the receipt swipe.
        public void PlaySwipe(bool right,bool acknowledgement=false)
        {
            if(!Application.isPlaying || suspended || muteCards || cardSource==null) return;
            AudioClip clip=acknowledgement?acknowledgeClip:null;
            if(clip==null && swipeClips!=null && swipeClips.Length>0)
                clip=swipeClips[nextSwipe++%swipeClips.Length];
            if(clip==null) return;
            cardSource.panStereo=right?.12f:-.12f;
            cardSource.PlayOneShot(clip,cardVolume);
        }
        void OnApplicationPause(bool paused)
        {
            suspended=paused;
            if(paused) { musicSource?.Pause(); cardSource?.Pause(); }
            else { musicSource?.UnPause(); cardSource?.UnPause(); }
        }
        void OnDisable() { musicSource?.Stop(); cardSource?.Stop(); fade=0; }
        void OnEnable()
        {
            if(Application.isPlaying && started) BeginMusic();
        }
    }
}
