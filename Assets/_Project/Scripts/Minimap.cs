using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using csDelaunay;
using UnityEngine;

namespace _Project.Scripts
{
    public class Minimap : MonoBehaviour
    {
        [Header("Main Camera")]
        public Camera m_camera;
    
        [Header("Map Generation Info")] 
        public int m_nbOfSites;
        public string m_customSeed;
        public bool m_useCustomSeed;
        public Vector2 m_minimapBorders;
        public int m_lloydIterations;
    
        [Header("Prefabs And Container")]
        public GameObject m_sitePrefab;
        public Transform m_container;

        private Dictionary<Vector2f, MinimapSite> m_sitesDictionary;
        private MinimapLinesRenderer m_minimapLinesRenderer;

        private struct MapData
        {
            public Vector3 m_startPosition;
            public Vector2 m_size;

        }

        private void Awake()
        {
            m_minimapLinesRenderer = GetComponent<MinimapLinesRenderer>();
        }

        private void Start()
        {
            CreateMap();
        }

        private void CreateMap()
        {
            MapData mapData = CreateMinimapArea(m_camera, m_minimapBorders);
            string seed = GenerateSeed(m_customSeed, m_useCustomSeed);
            var sites = GenerateSites(m_nbOfSites, seed, mapData);
        
            m_sitesDictionary = GenerateMinimap(sites,mapData);
        }

        private static MapData CreateMinimapArea(Camera _camera, Vector2 _minimapBorder)
        {
            var mapData = new MapData();
        
            var bottomLeftCorner = _camera.ViewportToWorldPoint(new Vector2(0, 0))+ new Vector3(_minimapBorder.x,_minimapBorder.y);
            bottomLeftCorner.z = 0;

            var topRightCorner = _camera.ViewportToWorldPoint(new Vector2(1, 1))+ new Vector3(-_minimapBorder.x,-_minimapBorder.y);;
            topRightCorner.z = 0;

            mapData.m_startPosition = bottomLeftCorner;
            mapData.m_size = new Vector2
            {
                x = topRightCorner.x - bottomLeftCorner.x, 
                y = topRightCorner.y - bottomLeftCorner.y
            };
        
            return mapData;
        }
        
        private static string GenerateSeed(string _customSeed, bool _useCustomSeed)
        {
            if (_useCustomSeed && !string.IsNullOrEmpty(_customSeed)) return _customSeed;
            return (Time.time + Random.Range(0, 100)).ToString(CultureInfo.InvariantCulture);
        }
        
        private static List<Vector2f> GenerateSites(int _nbOfSites, string _seed, MapData _data)
        {
            var pseudoRandom = new System.Random(_seed.GetHashCode());

            var sites = new List<Vector2f>();
        
            for(var i = 0; i < _nbOfSites; i++)
            {
                var randomX = _data.m_startPosition.x + pseudoRandom.Next(0, 100)*.01f*_data.m_size.x;
                var randomY = _data.m_startPosition.y + pseudoRandom.Next(0, 100)*.01f*_data.m_size.y;

                var position = new Vector2f(randomX, randomY);
                sites.Add(position);
            }
            return sites;
        }

        private Dictionary<Vector2f,MinimapSite> GenerateMinimap(List<Vector2f> _sites, MapData _mapData)
        {
            var minimapZone = new Rectf(_mapData.m_startPosition.x, _mapData.m_startPosition.y, _mapData.m_size.x, _mapData.m_size.y);
            var diagram = new Voronoi(_sites, minimapZone,m_lloydIterations);
        
            var voronoiSites = diagram.SitesIndexedByLocation;
            voronoiSites = voronoiSites.OrderBy(obj => obj.Key.x).ToDictionary(obj => obj.Key, obj => obj.Value);
        
            var sitesDictionary = new Dictionary<Vector2f, MinimapSite>();
        
            var i = 0;
            foreach (var site in voronoiSites)
            {
                var sitePosition = site.Key;
                var item = Instantiate(m_sitePrefab, m_container);
                item.transform.position = new Vector3(sitePosition.x, sitePosition.y);
                var uiSite = item.GetComponent<MinimapSite>();
                uiSite.m_onMouseEnterEvent.AddListener(HandleSiteSelection);
                uiSite.m_onMouseExitEvent.AddListener(HandleSiteDeselection);
                uiSite.m_diagramSite = site.Value;
            
                uiSite.gameObject.name = "MinimapSite_" + i;
                sitesDictionary.Add(sitePosition, uiSite);
                i++;
            }

            return sitesDictionary;
        }

        private void HandleSiteDeselection(Site _selectedSite)
        {
            m_minimapLinesRenderer.HideAllLines();
        }

        private void HandleSiteSelection(Site _selectedSite)
        {
            var neighborSites = _selectedSite.NeighborSites();
            m_minimapLinesRenderer.DrawLines(_selectedSite.Coord, neighborSites);
        }

        private void Clear()
        {
            foreach (var VARIABLE in m_sitesDictionary)
            {
                Destroy(VARIABLE.Value.gameObject);
            }
            m_sitesDictionary.Clear();
        }
    
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Clear();
                CreateMap();
            }
        }

        private void OnDrawGizmos()
        {
            var bottomLeftCorner = m_camera.ViewportToWorldPoint(new Vector2(0, 0))+ new Vector3(m_minimapBorders.x,m_minimapBorders.y);
            bottomLeftCorner.z = 0;
            var bottomRightCorner = m_camera.ViewportToWorldPoint(new Vector2(1, 0))+ new Vector3(-m_minimapBorders.x,m_minimapBorders.y);
            bottomRightCorner.z = 0;
            var topLeftCorner = m_camera.ViewportToWorldPoint(new Vector2(0, 1))+ new Vector3(m_minimapBorders.x,-m_minimapBorders.y);
            topLeftCorner.z = 0;
            var topRightCorner = m_camera.ViewportToWorldPoint(new Vector2(1, 1))+ new Vector3(-m_minimapBorders.x,-m_minimapBorders.y);;
            topRightCorner.z = 0;
        
            const float sphereRadius = .2f;
        
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(bottomLeftCorner,sphereRadius);
            Gizmos.DrawSphere(bottomRightCorner,sphereRadius);
            Gizmos.DrawSphere(topLeftCorner,sphereRadius);
            Gizmos.DrawSphere(topRightCorner,sphereRadius);
        
            Gizmos.color = Color.green;
            Gizmos.DrawLine(bottomLeftCorner,topLeftCorner);
            Gizmos.DrawLine(bottomLeftCorner,bottomRightCorner);
            Gizmos.DrawLine(topRightCorner,topLeftCorner);
            Gizmos.DrawLine(bottomRightCorner,topRightCorner);
        }
    }

    
}