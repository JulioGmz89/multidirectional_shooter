using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectMayhem.Audio
{
    public class UISFX : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] private AudioEvent clickEvent = AudioEvent.UI_Click;
        [SerializeField] private AudioEvent hoverEvent = AudioEvent.UI_Hover;
        [SerializeField] private bool playHover = true;

        public void OnPointerClick(PointerEventData eventData)
        {
            SFX.Play(clickEvent);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playHover)
                SFX.Play(hoverEvent);
        }

        // Optional runtime controls
        public void SetClickEvent(AudioEvent evt) => clickEvent = evt;
        public void SetHoverEvent(AudioEvent evt) => hoverEvent = evt;
        public void SetPlayHover(bool value) => playHover = value;
    }
}
