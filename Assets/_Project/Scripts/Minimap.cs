using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PixelsForGlory.VoronoiDiagram;
using UnityEngine;

namespace _Project.Scripts
{
    public class Minimap : MonoBehaviour
    {
        [Header("Main Camera")]
        public Camera m_camera;
    
        [Header("Map Generation parameters")] 
        public int m_numberOfSites;
        public string m_currentSeed;
        public bool m_keepCurrentSeed;
        [Range(0f,.5f)]
        public float m_widthMargin;
        [Range(0f,.5f)]
        public float m_heightMargin;
        public int m_relaxationCycles = 1;
        
        [Header("Visualizer Options")] 
        public bool m_showVisualizer;

        [Header("Prefabs And Container")]
        public GameObject m_sitePrefab;
        public Transform m_container;

        private List<GameObject> m_sitesList;
        private MinimapLinesRenderer m_minimapLinesRenderer;
        private MapData m_mapData;

        private struct MapData
        {
            public Vector2 m_startPosition;
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
            m_mapData = CreateMinimapArea(m_camera, m_widthMargin,m_heightMargin);

            m_currentSeed = m_keepCurrentSeed && !string.IsNullOrEmpty(m_currentSeed) ? m_currentSeed : GenerateSeed();
            
            List<Vector2> sites = GenerateSites(m_numberOfSites, m_currentSeed, m_mapData);
        
            m_sitesList = GenerateMinimap(sites,m_mapData);
        }
        
        private static MapData CreateMinimapArea(Camera _camera, float _widthMargin,float _heightMargin)
        {
            var mapData = new MapData();

            var bottomLeftCorner = _camera.ViewportToScreenPoint(new Vector2(0, 0)+ new Vector2(_widthMargin, +_heightMargin));
            var topRightCorner = _camera.ViewportToScreenPoint(new Vector2(1, 1)+ new Vector2(-_widthMargin,-_heightMargin));

            mapData.m_startPosition = bottomLeftCorner;
            mapData.m_size = new Vector2
            {
                x = topRightCorner.x - bottomLeftCorner.x, 
                y = topRightCorner.y - bottomLeftCorner.y
            };
        
            return mapData;
        }
        
        private static string GenerateSeed()
        {
            return (Time.time + Random.Range(0, 100)).ToString(CultureInfo.InvariantCulture);
        }
        
        private static List<Vector2> GenerateSites(int _nbOfSites, string _seed, MapData _data)
        {
            var pseudoRandom = new System.Random(_seed.GetHashCode());

            var sites = new List<Vector2>();
        
            for(var i = 0; i < _nbOfSites; i++)
            {
                var randomX = pseudoRandom.Next(0, 100)*.01f*_data.m_size.x;
                var randomY = pseudoRandom.Next(0, 100)*.01f*_data.m_size.y;

                var position = new Vector2(randomX, randomY);
                sites.Add(position);
            }
            return sites;
        }

        private List<GameObject> GenerateMinimap(List<Vector2> _sites, MapData _mapData)
        {
            var widthInPixel = (int)_mapData.m_size.x;
            var heightInPixel = (int) _mapData.m_size.y;
            
            m_diagram = new VoronoiDiagram<Color>(new Rect(0,0, widthInPixel, heightInPixel));
            
            var voronoiDiagramSites = new List<VoronoiDiagramSite<Color>>();

            foreach (var site in _sites)
            {
                var color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                voronoiDiagramSites.Add(new VoronoiDiagramSite<Color>(site, color));
            }
            
            m_diagram.AddSites(voronoiDiagramSites);
            m_diagram.GenerateSites(m_relaxationCycles);

            var outImg = new Texture2D(widthInPixel, heightInPixel);
            
            outImg.SetPixels(m_diagram.Get1DSampleArray().ToArray());
            
            var index = 0;
            var list = new List<GameObject>();
            
            foreach (var generatedSiteKeyValuePair in m_diagram.GeneratedSites)
            {
                var sitePosition = m_camera.ScreenToWorldPoint(generatedSiteKeyValuePair.Value.Coordinate+_mapData.m_startPosition);
                sitePosition.z = -1;
                var item = Instantiate(m_sitePrefab, m_container);
                item.transform.position = sitePosition;
                item.name = "MinimapSite_" + index;
                list.Add(item);
                
                var uiSite = item.GetComponent<MinimapSite>();
                uiSite.m_onMouseEnterEvent.AddListener(HandleSiteSelection);
                uiSite.m_onMouseExitEvent.AddListener(HandleSiteDeselection);
                uiSite.m_diagramSite = generatedSiteKeyValuePair.Value;
                
                index++;
            }
            
            outImg.Apply();
            m_cellsVisualizer.materials[0].mainTexture = outImg;
            
            return list;
        }

        

        public MeshRenderer m_cellsVisualizer;

        private VoronoiDiagram<Color> m_diagram;

        private void HandleSiteSelection(VoronoiDiagramGeneratedSite<Color> _selectedSite)
        {
            var neighborSitesIndexes = _selectedSite.NeighborSites;
            var list = new List<Vector2>();
            foreach (var VARIABLE in neighborSitesIndexes)
            {
                if (!m_diagram.GeneratedSites.TryGetValue(VARIABLE, out var site)) continue;
                
                var position = m_camera.ScreenToWorldPoint(site.Coordinate+m_mapData.m_startPosition);
                position.z = -1;
                list.Add(position);
            }
            var startPosition = m_camera.ScreenToWorldPoint(_selectedSite.Coordinate+m_mapData.m_startPosition);
            startPosition.z = -1;
            
            m_minimapLinesRenderer.DrawLines(
                startPosition, list);
        }
        
        private void HandleSiteDeselection(VoronoiDiagramGeneratedSite<Color> _selectedSite)
        {
            m_minimapLinesRenderer.HideAllLines();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Clear();
                CreateMap();
            }
            m_cellsVisualizer.gameObject.SetActive(m_showVisualizer);
        }

        private void Clear()
        {
            m_minimapLinesRenderer.HideAllLines();
            foreach (var VARIABLE in m_sitesList)
            {
                Destroy(VARIABLE.gameObject);
            }
            m_sitesList.Clear();
        }
        
        private void OnDrawGizmos()
        {
            var bottomLeftCorner = m_camera.ViewportToWorldPoint(new Vector3(m_widthMargin, m_heightMargin));
            bottomLeftCorner.z = 0;
            var bottomRightCorner = m_camera.ViewportToWorldPoint(new Vector2(1, 0)+ new Vector2(-m_widthMargin,m_heightMargin));
            bottomRightCorner.z = 0;
            var topLeftCorner = m_camera.ViewportToWorldPoint(new Vector2(0, 1)+ new Vector2(m_widthMargin,-m_heightMargin));
            topLeftCorner.z = 0;
            var topRightCorner = m_camera.ViewportToWorldPoint(new Vector2(1, 1)+ new Vector2(-m_widthMargin,-m_heightMargin));
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

            var visualizerWidth = Vector3.Distance(topRightCorner, topLeftCorner);
            var visualizerheight = Vector3.Distance(topRightCorner, bottomRightCorner);
            m_cellsVisualizer.transform.localScale = new Vector3(visualizerWidth, visualizerheight, 1);
        }
    }
}