using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts
{
    public class MapLinesRenderer : MonoBehaviour
    {
        public GameObject m_linePrefab;
        public Transform m_container;

        public float m_tilingFactor = 4;
    
        private readonly List<LineRenderer> m_lineRenderers= new List<LineRenderer>();

        public void DrawLines(Vector2 _selectedSitePosition, List<Vector2> _neighborSitesPositions)
        {
            for (var i = m_lineRenderers.Count; i < _neighborSitesPositions.Count; i++)
            {
                var line = Instantiate(m_linePrefab, m_container);

                line.name = "lineRender_" + i;
                var newLineRenderer = line.GetComponent<LineRenderer>();
                m_lineRenderers.Add(newLineRenderer);
            }

            for (var i = 0; i < _neighborSitesPositions.Count; i++)
            {
                var startPosition = new Vector3(_selectedSitePosition.x, _selectedSitePosition.y);
                var endPosition = new Vector3(_neighborSitesPositions[i].x, _neighborSitesPositions[i].y);
            
                m_lineRenderers[i].SetPositions(new[]
                {
                    startPosition,
                    endPosition,
                });
            
                var lineLength = Vector3.Distance(startPosition, endPosition); 
                m_lineRenderers[i].enabled = true;
                m_lineRenderers[i].materials[0].mainTextureScale = new Vector2(lineLength*m_tilingFactor, 1);
            }
        }

        public void HideAllLines()
        {
            foreach (var VARIABLE in m_lineRenderers)
            {
                VARIABLE.enabled = false;
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HideAllLines();
            }
        }
    }
}