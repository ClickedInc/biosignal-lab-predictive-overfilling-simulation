using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OverfillingSimulation))]
public class OverfillingSimulationEditor : Editor {
    private OverfillingSimulation _simulation;
    private Transform _transform;
    private GUIStyle _labelStyle;

    void OnEnable() {
        if (_simulation == null) {
            _simulation = target as OverfillingSimulation;
            _transform = _simulation.transform;
            _labelStyle = new GUIStyle();
            _labelStyle.normal.textColor = Color.green;

            SceneView.onSceneGUIDelegate += OnScene;
        }
    }

    private void OnScene(SceneView scene) {
        var halfWidth = Mathf.Tan(_simulation.fov / 2 * Mathf.Deg2Rad);
        var hmdViewport = new Quad(
            new Vector3(-halfWidth, halfWidth, 1.0f),
            new Vector3(halfWidth, halfWidth, 1.0f),
            new Vector3(halfWidth, -halfWidth, 1.0f),
            new Vector3(-halfWidth, -halfWidth, 1.0f)
        );
        if (_simulation.hmd) {
            renderHMDFrustum(hmdViewport);
        }

        var qd = _transform.rotation;
        var minOverfillQuad = new Quad(
            vertexOfMinOverfillQuad(qd, hmdViewport.leftTop),
            vertexOfMinOverfillQuad(qd, hmdViewport.rightTop),
            vertexOfMinOverfillQuad(qd, hmdViewport.rightBottom),
            vertexOfMinOverfillQuad(qd, hmdViewport.leftBottom)
        );
        if (_simulation.minOverfillQuad) {
            renderMinOverfill(minOverfillQuad);
        }

        var left = Mathf.Min(new float[] {
            minOverfillQuad.leftTop.x, 
            minOverfillQuad.rightTop.x,
            minOverfillQuad.rightBottom.x,
            minOverfillQuad.leftBottom.x
        });
        var top = Mathf.Max(new float[] {
            minOverfillQuad.leftTop.y, 
            minOverfillQuad.rightTop.y,
            minOverfillQuad.rightBottom.y,
            minOverfillQuad.leftBottom.y
        });
        var right = Mathf.Max(new float[] {
            minOverfillQuad.leftTop.x, 
            minOverfillQuad.rightTop.x,
            minOverfillQuad.rightBottom.x,
            minOverfillQuad.leftBottom.x
        });
        var bottom = Mathf.Min(new float[] {
            minOverfillQuad.leftTop.y, 
            minOverfillQuad.rightTop.y,
            minOverfillQuad.rightBottom.y,
            minOverfillQuad.leftBottom.y
        });

        var width = right - left;
        var height = top - bottom;
        var halfDiff = Mathf.Abs(width - height) / 2;

        var overhead = 
            Mathf.Pow(Mathf.Max(right - left, top - bottom), 2) /
            Mathf.Pow(2 * halfWidth, 2) - 1;

        var overfillRect = width > height ? new Quad(
            new Vector3(left, top + halfDiff, 1.0f),
            new Vector3(right, top + halfDiff, 1.0f),
            new Vector3(right, bottom - halfDiff, 1.0f),
            new Vector3(left, bottom - halfDiff, 1.0f)
        ) : new Quad(
            new Vector3(left - halfDiff, top, 1.0f),
            new Vector3(right + halfDiff, top, 1.0f),
            new Vector3(right + halfDiff, bottom, 1.0f),
            new Vector3(left - halfDiff, bottom, 1.0f)
        );
        if (_simulation.overfillRect) {
            renderOverfillRect(overfillRect, overhead);
        }
    }

    private Vector3 vertexOfMinOverfillQuad(Quaternion predictError, Vector3 direction) {
        return predictError * (1.0f / (predictError * direction).z * direction);
    }

    private void renderHMDFrustum(Quad viewport) {
        Handles.color = new Color(1.0f, 1.0f, 1.0f, 0.4f);
        Handles.DrawLines(viewport.sides(_transform.position, _transform.rotation, 2.0f));
        
        Handles.color = Color.white;
        Handles.DrawSolidDisc(_transform.position + _transform.forward, _transform.forward, 0.05f);

        Handles.color = new Color(1.0f, 1.0f, 1.0f, 0.4f);
        Handles.DrawSolidRectangleWithOutline(
            viewport.rect(_transform.position, _transform.rotation), 
            new Color(1.0f, 1.0f, 1.0f, 0.4f), 
            Color.white
        );
    }

    private void renderMinOverfill(Quad quad) {
        Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
        Handles.DrawLines(quad.sides(_transform.position));
        
        Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
        Handles.DrawSolidRectangleWithOutline(
            quad.rect(_transform.position), 
            new Color(1.0f, 0.0f, 0.0f, 0.4f), 
            Color.red
        );
    }

    private void renderOverfillRect(Quad overfillRect, float overhead) {
        Handles.color = new Color(0.0f, 1.0f, 0.0f, 0.3f);
        Handles.DrawLines(overfillRect.sides(_transform.position));

        Handles.color = Color.green;
        Handles.DrawSolidDisc(_transform.position + Vector3.forward, Vector3.forward, 0.05f);

        Handles.color = new Color(0.0f, 1.0f, 0.0f, 0.4f);
        Handles.DrawSolidRectangleWithOutline(
            overfillRect.rect(_transform.position), 
            new Color(0.0f, 1.0f, 0.0f, 0.4f), 
            Color.green
        );

        var halfFov = _simulation.fov / 2;
        var stat = string.Format(
            "{0:P2} ({1:F1}, {2:F1}, {3:F1}, {4:F1})", 
            overhead,
            Mathf.Atan(Mathf.Abs(overfillRect.leftTop.x)) * Mathf.Rad2Deg - halfFov,
            Mathf.Atan(Mathf.Abs(overfillRect.leftTop.y)) * Mathf.Rad2Deg - halfFov,
            Mathf.Atan(Mathf.Abs(overfillRect.rightBottom.x)) * Mathf.Rad2Deg - halfFov,
            Mathf.Atan(Mathf.Abs(overfillRect.rightBottom.y)) * Mathf.Rad2Deg - halfFov
        );
        Handles.Label(_transform.position + overfillRect.rightTop + Vector3.right * 0.05f, stat, _labelStyle);
    }

    private struct Quad {
        public Vector3 leftTop;
        public Vector3 rightTop;
        public Vector3 rightBottom;
        public Vector3 leftBottom;

        public Quad(Vector3 lt, Vector3 rt, Vector3 rb, Vector3 lb) {
            leftTop = lt;
            rightTop = rt;
            rightBottom = rb;
            leftBottom = lb;
        }

        public Vector3[] rect(Vector3 origin) {
            return new Vector3[] { 
                origin + leftTop, 
                origin + rightTop, 
                origin + rightBottom, 
                origin + leftBottom 
            };
        }

        public Vector3[] rect(Vector3 origin, Quaternion rotation) {
            return new Vector3[] { 
                origin + rotation * leftTop, 
                origin + rotation * rightTop, 
                origin + rotation * rightBottom, 
                origin + rotation * leftBottom 
            };
        }

        public Vector3[] sides(Vector3 from, float length = 1.0f) {
            return new Vector3[] {
                from, from + leftTop * length,
                from, from + rightTop * length,
                from, from + rightBottom * length,
                from, from + leftBottom * length
            };
        }

        public Vector3[] sides(Vector3 from, Quaternion rotation, float length = 1.0f) {
            return new Vector3[] {
                from, from + rotation * leftTop * length,
                from, from + rotation * rightTop * length,
                from, from + rotation * rightBottom * length,
                from, from + rotation * leftBottom * length
            };
        }
    }
}
