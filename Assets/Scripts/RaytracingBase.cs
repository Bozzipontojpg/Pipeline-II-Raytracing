using UnityEngine;
using System.IO;

public class RaytracingBase : MonoBehaviour {
	public int width = 1024;
	public int height = 1024;

	[Range(0.01f, 0.99f)]
	public float shadowHardness = 0.5f;

	public Light[] lights;

	Vector3 pointZeroNear;
	Vector3 pointZeroFar;

	float Hnear, Wnear, Hfar, Wfar, aspectRatio;

	void CalculateFrustum(Texture2D texture) {
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

	void CalculateRay(int pixelX, int pixelY, out Vector3 rayOrig, out Vector3 rayDir) {
		rayOrig = pointZeroNear + new Vector3((Wnear / (float) width) * (float) pixelX, (Hnear / (float) height) * (float) pixelY, 0.0f);

		Vector3 rayDest = pointZeroFar + new Vector3((Wfar / (float) width) * (float) pixelX, (Hfar / (float) height) * (float) pixelY, 0.0f);

		rayDir = rayDest - rayOrig;
		rayDir.Normalize(); 
	}
	
	Color Trace(Ray ray, int step) {
		RaycastHit raycastHit;

		if(!Physics.Raycast(ray, out raycastHit, 200f)) {
			return new Color(0.25f, 0.25f, 0.25f);
			// alternatives: getting the scene cubemap
		}

		Renderer objectRenderer = raycastHit.transform.gameObject.GetComponent<Renderer>();
		if(objectRenderer == null) {
			return Color.black;
		}

		Material objectMaterial = objectRenderer.material;
		Vector2 uv = raycastHit.textureCoord;

		Color materialColor = objectMaterial.color; // the teacher recommended using a Vector3 instead of a Color
		float materialSmoothness = objectMaterial.GetFloat("_Glossiness");
		float materialMetallic = objectMaterial.GetFloat("_Metallic");

		// alternatives: consult the object's texture coordinates

		float shadow = 1f;
		float attenuation = 1f;

		Light light = lights[0];
		Vector3 lightDirection = Vector3.zero;

		switch(light.type) {
			case LightType.Directional:
				lightDirection = -light.transform.forward;
				break;

			case LightType.Point:
				lightDirection = (light.transform.position - raycastHit.point).normalized;
				attenuation = 1f / Mathf.Pow(raycastHit.distance, 2f);
				break;
		}

		Ray rayTowardsLight = new Ray(raycastHit.point, lightDirection);
		if(Physics.Raycast(rayTowardsLight, 200f)) {
			shadow = shadowHardness;
		}

		float normalDotLight = Mathf.Clamp01(Vector3.Dot(raycastHit.normal, lightDirection));
		Color diffuseColor = normalDotLight * materialColor * light.color * attenuation * shadow;

    //	float normalDotView = Mathf.Clamp01(Vector3.Dot());
		Color specularColor;

		return diffuseColor;
	}

	void Start() {
		Texture2D texture = new Texture2D(width, height);

		CalculateFrustum(texture);

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				Vector3 rayOri = Vector3.zero;
				Vector3 rayDir = Vector3.zero;

				CalculateRay(x, y, out rayOri, out rayDir);
				
				Ray ray = new Ray(rayOri, rayDir);
				
				// Raycast testando colisão com objetos na cena

				Color color = Trace(ray, 0);

				texture.SetPixel(x, y, color);
			}
		}

		texture.Apply();

		byte [] bytes = ImageConversion.EncodeToPNG(texture);
		File.WriteAllBytes(Application.dataPath + "/SavedScreen.png", bytes);
		Debug.Log("Image created.");
	}
}