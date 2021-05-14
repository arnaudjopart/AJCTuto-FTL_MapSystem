using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using csDelaunay;
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
        public Vector2 m_minimapBorders;
        public int m_lloydIterations;
    
        [Header("Prefabs And Container")]
        public GameObject m_sitePrefab;
        public Transform m_container;

        private List<MinimapSite> m_sitesDictionary;
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
            m_mapData = CreateMinimapArea(m_camera, m_widthMargin,m_heightMargin);
            m_currentSeed = GenerateSeed(m_currentSeed, m_keepCurrentSeed);
            List<Vector2f> sites = GenerateSites(m_numberOfSites, m_currentSeed, m_mapData);
        
            m_sitesDictionary = GenerateMinimap(sites,m_mapData);
        }

        private MapData m_mapData { get; set; }

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

        private List<MinimapSite> GenerateMinimap(List<Vector2f> _sites, MapData _mapData)
        {

            var widthInPixel = (int)_mapData.m_size.x;
            var heightInPixel = (int) _mapData.m_size.y;
            
            m_diagram = new VoronoiDiagram<Color>(new Rect(0,0, widthInPixel, heightInPixel));
            
            var texturePoints = new List<VoronoiDiagramSite<Color>>();
            
            
            while(texturePoints.Count < m_numberOfSites)
            {
                float randX = Random.Range(0, widthInPixel);
                float randY = Random.Range(0, heightInPixel);

                var texturePoint = new Vector2(randX, randY);
                var point =  m_camera.ScreenToWorldPoint(new Vector2(randX, randY));
                point.z = 0;
                
                if(!texturePoints.Any(item => item.Coordinate == texturePoint))
                {
                    var color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                    texturePoints.Add(new VoronoiDiagramSite<Color>(texturePoint, color));
                }
            }
            
            
            m_diagram.AddSites(texturePoints);
            m_diagram.GenerateSites(2);
            
            var outImg = new Texture2D(
                widthInPixel, 
                    heightInPixel);
            
            outImg.SetPixels(m_diagram.Get1DSampleArray().ToArray());

            var index = 0;
            var sitesDictionary = new List<MinimapSite>();
            
            foreach (var VARIABLE in m_diagram.GeneratedSites)
            {
                for (var i = (int) VARIABLE.Value.Coordinate.x - 10; i < (int) VARIABLE.Value.Coordinate.x + 10; i++)
                {
                    for (var j = (int) VARIABLE.Value.Coordinate.y - 10; j < (int) VARIABLE.Value.Coordinate.y + 10; j++)
                    {
                        outImg.SetPixel(i, j, Color.red);
                    }
                }
                
                var sitePosition = m_camera.ScreenToWorldPoint(VARIABLE.Value.Coordinate+(Vector2)_mapData.m_startPosition);
                sitePosition.z = -1;
                var item = Instantiate(m_sitePrefab, m_container);
                item.transform.position = sitePosition;
                
                var uiSite = item.GetComponent<MinimapSite>();
                uiSite.m_onMouseEnterEvent.AddListener(HandleSiteSelection);
                uiSite.m_onMouseExitEvent.AddListener(HandleSiteDeselection);
                uiSite.m_diagramSite = VARIABLE.Value;
                uiSite.gameObject.name = "MinimapSite_" + index;
                sitesDictionary.Add(uiSite);
                index++;
            }
            
            outImg.Apply();
            m_quad.materials[0].mainTexture = outImg;
            
            return sitesDictionary;
        }

        public MeshRenderer m_quad;

        public VoronoiDiagram<Color> m_diagram { get; set; }

        private void HandleSiteSelection(VoronoiDiagramGeneratedSite<Color> _selectedSite)
        {
            var neighborSitesIndexes = _selectedSite.NeighborSites;
            var list = new List<Vector2>();
            foreach (var VARIABLE in neighborSitesIndexes)
            {
                if(m_diagram.GeneratedSites.TryGetValue(VARIABLE, out var site))
                {
                    var position = m_camera.ScreenToWorldPoint(site.Coordinate+(Vector2)m_mapData.m_startPosition);
                    position.z = -1;
                    list.Add(position);
                }
            }
            var startPosition = m_camera.ScreenToWorldPoint(_selectedSite.Coordinate+(Vector2)m_mapData.m_startPosition);
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
        }

        private void Clear()
        {
            m_minimapLinesRenderer.HideAllLines();
            foreach (var VARIABLE in m_sitesDictionary)
            {
                Destroy(VARIABLE.gameObject);
            }
            m_sitesDictionary.Clear();
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
            m_quad.transform.localScale = new Vector3(visualizerWidth, visualizerheight, 1);
        }
    }
}