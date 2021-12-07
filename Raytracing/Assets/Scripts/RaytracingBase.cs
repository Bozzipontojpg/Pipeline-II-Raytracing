using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class RaytracingBase : MonoBehaviour
{
	public int width = 1024;
	public int height = 768;

    [Range(0.1f, 0.9f)]
    public float ShadowHardness = 0.5f;

    public Transform LightList;

	Vector3 pointZeroNear;
	Vector3 pointZeroFar;

	float Hnear, Wnear, Hfar, Wfar, aspectRatio;

	void CalculateFrustum(Texture2D texture)
	{
		width = texture.width;
		height = texture.height;
			
		float angle = Mathf.Tan(0.5f * Camera.main.fieldOfView * Mathf.Deg2Rad);

		aspectRatio = (float) width / (float) height;

		Hnear = 2.0f * angle * Camera.main.nearClipPlane;
		Wnear = Hnear * aspectRatio;

		Hfar =  2.0f * angle * Camera.main.farClipPlane;
		Wfar = Hfar * aspectRatio;

		Vector3 near = Camera.main.transform.position + new Vector3(0.0f, 0.0f, Camera.main.nearClipPlane);
		Vector3 far = Camera.main.transform.position + new Vector3(0.0f, 0.0f, Camera.main.nearClipPlane + Camera.main.farClipPlane);

		pointZeroNear = near + new Vector3(-Wnear * 0.5f, Hnear * 0.5f, 0.0f);
		pointZeroFar = far + new Vector3(-Wfar * 0.5f, -Hfar * 0.5f, 0.0f);
	}

	void CalculateRay(int pixelX, int pixelY, out Vector3 rayOrig, out Vector3 rayDir)
	{
		rayOrig = pointZeroNear + new Vector3((Wnear / (float) width) * (float) pixelX, (Hnear / (float) height) * (float) pixelY, 0.0f);

		Vector3 rayDest = pointZeroFar + new Vector3((Wfar / (float) width) * (float) pixelX, (Hfar / (float) height) * (float) pixelY, 0.0f);

		rayDir = rayDest - rayOrig;
		rayDir.Normalize(); 
	}

    Vector3 MultiplyVector(Vector3 first, Vector3 second)
    {
        Vector3 result = Vector3.zero;

        result.x = first.x * second.x;
        result.y = first.y * second.y;
        result.z = first.z * second.z;

        return result;
    }
	
	Vector3 trace(Ray ray, int step)
	{
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 200.0f) == false)
            return new Vector3(0.25f, 0.25f, 0.25f); // Pode testar colisão com o CubeMap

        Vector3 uv = hit.textureCoord;
        Material objMaterial = hit.collider.gameObject.GetComponent<Renderer>().material;

        // Para fazer conta de luz
        Vector3 materialColor = new Vector3(objMaterial.color.r, objMaterial.color.g, objMaterial.color.b);
        float smoothness = objMaterial.GetFloat("_Glossiness");
        float metallic = objMaterial.GetFloat("_Metallic");

        Transform lightObj = LightList.GetChild(0); // Pegando a primeira luz
        Light light = lightObj.GetComponent<Light>();
        Vector3 lightColor = new Vector3(light.color.r, light.color.g, light.color.b) * light.intensity;

        // Calculando o vetor direção para luz
        Vector3 lightVector = Vector3.zero;
        if (light.type == LightType.Directional)
            lightVector = lightObj.forward * -1.0f;
        else if (light.type == LightType.Point)
            lightVector = (lightObj.position - hit.point).normalized;

        float shadow = 1.0f;
        Ray rayToLight = new Ray(hit.point, lightVector);
        if (Physics.Raycast(ray, 1000.0f))
            shadow = ShadowHardness;

        float NdotL = Mathf.Clamp(Vector3.Dot(hit.normal, lightVector), 0.0f, 1.0f);
        Vector3 diffuseColor = NdotL * MultiplyVector(materialColor, lightColor) * shadow;

        return diffuseColor;
	}

	void Start()
	{
		Texture2D texture = new Texture2D(width, height);

		CalculateFrustum(texture);

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				Vector3 rayOri = Vector3.zero;
				Vector3 rayDir = Vector3.zero;

				CalculateRay(x, y, out rayOri, out rayDir);

				Ray ray = new Ray(rayOri, rayDir);
				
				// Raycast testando colisão com objetos na cena
				Vector3 result = trace(ray, 0);
				
				Color color = new Color(result.x, result.y, result.z);

				texture.SetPixel(x, y, color);
			}
		}

        Debug.Log("Finalizou a imagem");
        texture.Apply();

        byte [] bytes = ImageConversion.EncodeToPNG(texture);
		File.WriteAllBytes(Application.dataPath + "/SavedScreen.png", bytes);
	}
}