using System.Collections.Generic;
using csDelaunay;
using UnityEngine;

public class MinimapLinesRenderer : MonoBehaviour
{
    public GameObject m_linePrefab;
    public Transform m_container;
    
    private readonly List<LineRenderer> m_lineRenderers= new List<LineRenderer>();

    public void DrawLines(Vector2f _arg0Coord, List<Site> _neighborSites)
    {
        for (var i = m_lineRenderers.Count; i < _neighborSites.Count; i++)
        {
            var line = Instantiate(m_linePrefab, m_container);

            line.name = "lineRender_" + i;
            var newLineRenderer = line.GetComponent<LineRenderer>();
            m_lineRenderers.Add(newLineRenderer);
        }

        for (var i = 0; i < _neighborSites.Count; i++)
        {
            m_lineRenderers[i].SetPositions(new[]
            {
                new Vector3(_arg0Coord.x,_arg0Coord.y),
                new Vector3(_neighborSites[i].Coord.x,_neighborSites[i].Coord.y),
            });
            m_lineRenderers[i].enabled = true;
            //m_lineRenderers[i].materials[0].mainTextureScale
        }
    }

    public void HideAllLines()
    {
        foreach (var VARIABLE in m_lineRenderers)
        {
            VARIABLE.enabled = false;
        }
    }
}