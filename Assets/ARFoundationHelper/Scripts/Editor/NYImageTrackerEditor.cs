using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
[CustomEditor(typeof(NYImageTracker))]
public class NYImageTrackerEditor : Editor
{
    Texture2D _lastTexture;
    Vector2 _lastTrackerSize = new Vector2(1.0f, 1.0f);
    float _sizeRatio = 1.0f;
    int inspectorCounter = -1; // check if it first time selected

    public override VisualElement CreateInspectorGUI()
    {
        NYImageTracker targetScript = (NYImageTracker)target;
        GameObject targetObj = targetScript.gameObject;


        // check component
        if(!targetObj.GetComponent<MeshRenderer>())
        {
            targetObj.AddComponent<MeshRenderer>();

            targetObj.GetComponent<MeshRenderer>().material = new Material(Resources.Load<ARFoundationHelperSettings>("HelperSettings").DefaultImageTrackerMaterial);
        }

        if(!targetObj.GetComponent<MeshFilter>())
        {
            targetObj.AddComponent<MeshFilter>();

            targetObj.GetComponent<MeshFilter>().mesh = CreateTrackerMesh();
        }

        return base.CreateInspectorGUI();
    }

    Mesh CreateTrackerMesh ()
    {
        Mesh _mesh = new Mesh();

        Vector3[] _verts = new Vector3[4];
        _verts[0] = new Vector3(-0.5f, 0.0f, 0.5f);
        _verts[1] = new Vector3(0.5f, 0.0f, 0.5f);
        _verts[2] = new Vector3(-0.5f, 0.0f, -0.5f);
        _verts[3] = new Vector3(0.5f, 0.0f, -0.5f);

        Vector2[] _uvs = new Vector2[4];
        _uvs[0] = new Vector2(0.0f, 1.0f);
        _uvs[1] = new Vector2(1.0f, 1.0f);
        _uvs[2] = new Vector2(0.0f, 0.0f);
        _uvs[3] = new Vector2(1.0f, 0.0f);

        int[] _tris = new int[6];
        _tris[0] = 0;
        _tris[1] = 1;
        _tris[2] = 2;
        _tris[3] = 1;
        _tris[4] = 3;
        _tris[5] = 2;

        _mesh.vertices = _verts;
        _mesh.uv = _uvs;
        _mesh.triangles = _tris;
        _mesh.name = "generated_tracker_mesh";

        return _mesh;
    }

    void UpdateMeshShape (float meshWidth, float meshHeight)
    {
        // check if there's mesh
        GameObject targetObj = ((NYImageTracker)target).gameObject;

        if(targetObj.GetComponent<MeshFilter>().sharedMesh == null)
        {
            targetObj.GetComponent<MeshFilter>().sharedMesh = CreateTrackerMesh();
        }

        // update vertices
        Mesh targetMesh = targetObj.GetComponent<MeshFilter>().sharedMesh;

        Vector3[] _verts = new Vector3[4];
        _verts[0] = new Vector3(-0.5f * meshWidth, 0.0f, 0.5f * meshHeight);
        _verts[1] = new Vector3(0.5f * meshWidth, 0.0f, 0.5f * meshHeight);
        _verts[2] = new Vector3(-0.5f * meshWidth, 0.0f, -0.5f * meshHeight);
        _verts[3] = new Vector3(0.5f * meshWidth, 0.0f, -0.5f * meshHeight);

        targetMesh.vertices = _verts;
        targetMesh.RecalculateBounds();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        NYImageTracker _target = target as NYImageTracker;

        // first frame of OnInspector!
        if (inspectorCounter < 0)
        {
            _lastTexture = _target.trackerImage;
            _lastTrackerSize = _target.physicalSize;
            _sizeRatio = _target.physicalSize.y / _target.physicalSize.x;

            ++inspectorCounter;
        }
        else if (inspectorCounter > 10000)
        {
            inspectorCounter = 100; // prevent int overflow
        }



        if (_target.trackerImage != _lastTexture)
        {
            OnTextureChange();
            UpdateMeshShape(_lastTrackerSize.x, _lastTrackerSize.y);
        }
        else if (_target.physicalSize != _lastTrackerSize)
        {
            if (_target.physicalSize.x != _lastTrackerSize.x)
            {
                _target.physicalSize.y = _target.physicalSize.x * _sizeRatio;
            }
            else if (_target.physicalSize.y != _lastTrackerSize.y)
            {
                _target.physicalSize.x = _target.physicalSize.y / _sizeRatio;
            }

            _lastTrackerSize = _target.physicalSize;
            UpdateMeshShape(_lastTrackerSize.x, _lastTrackerSize.y);
        }
    }

    void OnTextureChange()
    {
        NYImageTracker targetScript = (NYImageTracker)target;
        GameObject targetObj = targetScript.gameObject;

        if (targetScript.trackerImage != _lastTexture)
        {
            _lastTexture = targetScript.trackerImage;

            targetObj.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = _lastTexture;

            // change size ratio
            Vector2 textureSize = GetTextureSize(_lastTexture);

            Debug.Log("TEXTURE SIZE: " + textureSize);

            _sizeRatio = (float)textureSize.y / (float)textureSize.x;

            // landscape
            if(textureSize.x > textureSize.y)
            {
                targetScript.physicalSize = new Vector2(1.0f, _sizeRatio);
            }
            else // portrait
            {
                targetScript.physicalSize = new Vector2(1.0f / _sizeRatio, 1.0f);
            }

            targetObj.transform.localScale = Vector3.one;

            // update mesh
            _lastTrackerSize = targetScript.physicalSize;
        }
    }

    Vector2 GetTextureSize(Texture2D targetTexture)
    {
        Vector2 returnValue = Vector2.zero;

        string assetPath = AssetDatabase.GetAssetPath(targetTexture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            object[] args = new object[2] { 0, 0 };
            MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Invoke(importer, args);

            returnValue.x = (int)args[0];
            returnValue.y = (int)args[1];

            return returnValue;
        }

        return new Vector2(-1.0f, -1.0f);
    }
}