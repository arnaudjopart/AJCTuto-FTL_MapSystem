using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using csDelaunay;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    public int m_nbOfSites;

    public Vector2 m_mapStartPosition;
    public Vector2 m_mapSize;

    public string m_seed;

    public bool m_useRandomSeed;

    public int m_LloydIterations;

    public float m_maxDistance;

    public System.Action OnMapGenerate;
    
	void Start () {

        OnMapGenerate = null;
        

    }


    public void GenerateMap(int _nbOfSites, float _maxDistance)
    {
        m_maxDistance = _maxDistance;
        
        m_seed = (Time.time + Random.Range(0,100)).ToString(CultureInfo.InvariantCulture);
        print("Generate new Map: "+m_seed);

        List<Vector2f> sites = new List<Vector2f>();

        m_sortedSiteCoords = new List<Vector2f>();
                

        System.Random pseudoRandom = new System.Random(m_seed.GetHashCode());

        Rectf borders = new Rectf(m_mapStartPosition.x, m_mapStartPosition.y, m_mapSize.x, m_mapSize.y);

        for(int i = 0; i < _nbOfSites; i++)
        {
            float randomX = m_mapStartPosition.x + pseudoRandom.Next(0,100) * m_mapSize.x*.01f;
            float randomY = m_mapStartPosition.y + pseudoRandom.Next(0, 100) * m_mapSize.y*.01f;

            Vector2f position = new Vector2f(randomX, randomY);
            //print(position);
            sites.Add(position);
        }

        // Order List by x coord 

        //sites.Sort(CompareSiteByX);

        m_graph = new Voronoi(sites, borders,m_LloydIterations);
        SortSites(m_graph);

        if(OnMapGenerate != null)
        {
            OnMapGenerate();
        }

    }

    public Vector2f GetSitePositionByIndex(int _index)
    {
        return m_sortedSiteCoords[_index];
    }
    public List<Vector2f> GetNeighbors(int _index)
    {

        Vector2f coord = _index<m_sortedSiteCoords.Count?m_sortedSiteCoords[_index]:Vector2f.zero;
        List<Vector2f> list = m_graph.NeighborSitesForSite(coord);
        if (list != null)
        {
            for (int i = list.Count-1; i >= 0; i--)
            {
                if (Vector2f.DistanceSquare(coord, list[i]) > m_maxDistance * m_maxDistance)
                {
                    list.RemoveAt(i);
                    //print("Remove Site");
                }
            }
        }
        
        return list;
    }

    public List<Vector2f> GetNeighbors(Vector2f _coord)
    {
        List<Vector2f> list = m_graph.NeighborSitesForSite(_coord);
        if (list != null)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (Vector2f.DistanceSquare(_coord, list[i]) > m_maxDistance * m_maxDistance)
                {
                    list.RemoveAt(i);
                    //print("Remove Site");
                }
            }
        }

        return list;
    }

    private void SortSites(Voronoi _graph)
    {
        
        m_sortedSiteCoords = _graph.SiteCoords();
        m_sortedSiteCoords.Sort(CompareSiteByX);

    }


    private int CompareSiteByX(Vector2f a, Vector2f b)
    {
        return (int)(a.x - b.x);

    }
    public List<Vector2f> m_sortedSiteCoords;

    public Voronoi m_graph;
}
