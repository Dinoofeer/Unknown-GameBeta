using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

[RequireComponent(typeof(UIDocument), typeof(VideoPlayer))]
public class MenuVideoBackground : MonoBehaviour
{
    [SerializeField] private Texture2D fallbackImage;
    [SerializeField, Min(1f)] private float startupTimeout = 10f;
    [SerializeField] private bool muteVideo = true;

    private VideoPlayer player;
    private Image videoImage;
    private RenderTexture videoTexture;
    private float startupTime;
    private bool waitingForFrame;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        Image fallback = root.Q<Image>("backgroundFallback");
        videoImage = root.Q<Image>("backgroundVideo");
        fallback.image = fallbackImage;
        fallback.scaleMode = ScaleMode.ScaleAndCrop;
        videoImage.scaleMode = ScaleMode.ScaleAndCrop;
        videoImage.AddToClassList("is-hidden");

        player = GetComponent<VideoPlayer>();
        player.source = VideoSource.VideoClip;
        player.playOnAwake = false;
        player.isLooping = true;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.audioOutputMode = muteVideo ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;

        if (player.clip == null)
        {
            Debug.LogWarning("Menú: falta el video de fondo; se mostrará la imagen de respaldo.", this);
            return;
        }

        videoTexture = new RenderTexture((int)player.clip.width, (int)player.clip.height, 0)
        {
            name = "MenuVideoRuntime",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        videoTexture.Create();
        videoImage.image = videoTexture;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = videoTexture;
        player.prepareCompleted += OnPrepared;
        player.frameReady += OnFirstFrame;
        player.errorReceived += OnError;
        player.sendFrameReadyEvents = true;
        waitingForFrame = true;
        startupTime = Time.realtimeSinceStartup;
        player.Prepare();
    }

    private void Update()
    {
        if (waitingForFrame && Time.realtimeSinceStartup - startupTime > startupTimeout)
            OnError(player, "El video no entregó su primer fotograma a tiempo.");
    }

    private void OnPrepared(VideoPlayer source) => source.Play();

    private void OnFirstFrame(VideoPlayer source, long frame)
    {
        waitingForFrame = false;
        videoImage.RemoveFromClassList("is-hidden");
        source.sendFrameReadyEvents = false;
    }

    private void OnError(VideoPlayer source, string message)
    {
        waitingForFrame = false;
        videoImage.AddToClassList("is-hidden");
        source.Stop();
        Debug.LogWarning($"Menú: se usará la imagen de respaldo. {message}", this);
    }

    private void OnDisable()
    {
        waitingForFrame = false;
        if (player != null)
        {
            player.prepareCompleted -= OnPrepared;
            player.frameReady -= OnFirstFrame;
            player.errorReceived -= OnError;
            player.sendFrameReadyEvents = false;
            player.Stop();
            player.targetTexture = null;
        }

        if (videoImage != null)
        {
            videoImage.AddToClassList("is-hidden");
            videoImage.image = null;
        }

        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
            videoTexture = null;
        }
    }
}
