using PixelsForGlory.VoronoiDiagram;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Scripts
{
    public class MapSite: MonoBehaviour
    {
        public VoronoiDiagramGeneratedSite<Color> m_diagramSite;
        public GameObject m_onHoverAnim;
    
        public SiteSelectionEvent m_onMouseEnterEvent;
        public SiteSelectionEvent m_onMouseExitEvent;
    
        private void Awake()
        {
            m_onHoverAnim.SetActive(false);
        
            m_onMouseEnterEvent = new SiteSelectionEvent();
            m_onMouseExitEvent = new SiteSelectionEvent();
        }

        private void OnMouseEnter()
        {
            m_onMouseEnterEvent.Invoke(m_diagramSite);
            m_onHoverAnim.SetActive(true);
        }

        private void OnMouseExit()
        {
            m_onMouseExitEvent.Invoke(null);
            m_onHoverAnim.SetActive(false);
        }
    }

    public class SiteSelectionEvent  : UnityEvent<VoronoiDiagramGeneratedSite<Color>> {}
}