using UnityEngine;

namespace Mandarin {
    public class MeshUtils {

        public enum Orientation {
            Horizontal = 0,
            Vertical = 1,
        }
        
        public static Mesh CreatePlane(float width,
                                    float length,
                                    int widthSegments,
                                    int lengthSegments,
                                    Orientation orientation =  Orientation.Horizontal,
                                    Vector2 anchor =           default(Vector2)) {

            widthSegments = Mathf.Clamp(widthSegments, 1, 254);
            lengthSegments = Mathf.Clamp(lengthSegments, 1, 254);
            anchor.x = Mathf.Clamp01(anchor.x);
            anchor.y = Mathf.Clamp01(anchor.y);

            Vector2 anchorOffset = new Vector2((width * anchor.x) - (width * 0.5f),
                                            (length * anchor.y) - (length * 0.5f));

            Mesh m = new Mesh();

            int hCount2 = widthSegments+1;
            int vCount2 = lengthSegments+1;
            int numTriangles = widthSegments * lengthSegments * 6;
            int numVertices = hCount2 * vCount2;

            Vector3[] vertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[numTriangles];
            Vector4[] tangents = new Vector4[numVertices];
            Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

            int index = 0;
            float uvFactorX = 1.0f;
            float uvFactorY = 1.0f;
            float scaleX = width/widthSegments;
            float scaleY = length/lengthSegments;

            for (float y = 0.0f; y < vCount2; y++) {
                for (float x = 0.0f; x < hCount2; x++) {
                    if (orientation == Orientation.Horizontal) {
                        vertices[index] = new Vector3(x*scaleX - width/2f - anchorOffset.x, 0.0f, y*scaleY - length/2f - anchorOffset.y);
                    } else {
                        vertices[index] = new Vector3(x*scaleX - width/2f - anchorOffset.x, y*scaleY - length/2f - anchorOffset.y, 0.0f);
                    }
                    tangents[index] = tangent;
                    uvs[index++] = new Vector2(x * uvFactorX, y * uvFactorY);
                }
            }

            index = 0;

            for (int y = 0; y < lengthSegments; y++) {
                for (int x = 0; x < widthSegments; x++) {
                    triangles[index]   = (y     * hCount2) + x;
                    triangles[index+1] = ((y+1) * hCount2) + x;
                    triangles[index+2] = (y     * hCount2) + x + 1;

                    triangles[index+3] = ((y+1) * hCount2) + x;
                    triangles[index+4] = ((y+1) * hCount2) + x + 1;
                    triangles[index+5] = (y     * hCount2) + x + 1;
                    index += 6;
                }
            }

            m.vertices = vertices;
            m.uv = uvs;
            m.triangles = triangles;
            m.tangents = tangents;
            m.RecalculateNormals();
            m.RecalculateBounds();

            return m;
        }

        static public Mesh DuplicateMesh(Mesh original) {
            Mesh dup = new Mesh();
            dup.vertices = original.vertices;
            dup.triangles = original.triangles;
            dup.uv = original.uv;
            dup.normals = original.normals;

            dup.colors = new Color[original.colors.Length];
            for (int i=0; i<original.colors.Length; i++) {
                dup.colors[i] = original.colors[i];
            }

            dup.tangents = original.tangents;
            return dup;
        }
    }
}
