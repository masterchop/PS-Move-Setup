// TODO: create a more realistic and at scale 3D model for the PS Move
// TODO: separate the elements from the DK2 3D model and give them a specific material (plastic, glass, foam)

using UnityEngine;
using System;
using System.Collections.Generic;

public class MoveController : MonoBehaviour 
{
	public List<MoveManager> moves = new List<MoveManager>();
	
	HornCoregistration Coregistration = new HornCoregistration();
	
	public Vector3 DisplayedPosition;
	public Matrix4x4 TransformMatrix;
	public Vector3 CorrectedPosition;
	public Vector3 RotatedPosition;
	
	private DK2Controller dk2;
	
	GUIStyle style;

	void Start() 
	{
		style = new GUIStyle();
		style.normal.textColor = Color.black;
	
		int count = MoveManager.GetNumConnected();
		
		for (int i = 0; i < count; i++) 
		{
			MoveManager move = gameObject.AddComponent<MoveManager>();
			
			if (!move.Init(i)) 
			{	
				Destroy(move);
				continue;
			}
			
			PSMoveConnectionType conn = move.ConnectionType;

			if (conn == PSMoveConnectionType.Unknown || conn == PSMoveConnectionType.USB) 
			{
				Destroy(move);
			}
			else 
			{
				moves.Add(move);
				move.OnControllerDisconnected += HandleControllerDisconnected;
				move.SetLED(Color.cyan);
				
				DisplayedPosition = new Vector3(0.0f, 1.0f, 0.0f);
				TransformMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
			}
		}
	}
	
	void Update() 
	{	
		dk2 = GameObject.FindWithTag("DK2").GetComponent<DK2Controller>();

		float angle = Quaternion.Angle(dk2.Orientation, Quaternion.AngleAxis(0, Vector3.forward));

		foreach (MoveManager move in moves) 
		{
			if (move.Disconnected) continue;

			//transform.rotation = Quaternion.Euler(0, -90, 0) * move.Orientation * Quaternion.Euler(-90, 0, 0);
			transform.rotation = move.Orientation;
			CorrectedPosition = TransformMatrix.MultiplyPoint3x4(move.Position);
			transform.position = CorrectedPosition + DisplayedPosition;

			if (move.GetButtonDown(PSMoveButton.Move)) {
				move.ResetOrientation();
			}

			// Correction of the position of the PS Move to cancel out the rotation from the DK2.
			RotatedPosition = Quaternion.Inverse(dk2.Orientation) * (move.Position - dk2.Position) + dk2.Position;

			//if (angle < 5.0f)
			//{
				if (Coregistration.Correlations.Count == 100 && !Coregistration.IsRegistered) {
					Coregistration.ComputeTransformMatrix();
					TransformMatrix = Coregistration.GetTransformMatrix();
				}

				Coregistration.AddCorrelation(angle, dk2.Position, RotatedPosition);

				/*Quaternion dummyRotation = Quaternion.Euler(47, 22, 13);
				Vector3 dummyTranslation = new Vector3(0.1f, 0.2f, 0.3f);
				Vector3 dummyPosition = dummyRotation * dk2.position + dummyTranslation;
				Coregistration.AddCorrelation(angle, dk2.position, dummyPosition);*/
			//}
		}
	}
	
	void HandleControllerDisconnected (object sender, EventArgs e)
	{
	}

	Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion angle)
	{
		return angle * (point - pivot) + pivot;
	}

	void OnGUI() 
	{
		foreach (MoveManager move in moves) 
		{
			string display = string.Format(
				"{0} : " +
				"Move (cm): {1:0.0} {2:0.0} {3:0.0} - " +
				"DK2 (cm): {4:0.0} {5:0.0} {6:0.0} - " +
				"Diff (mm): {7:0} {8:0} {9:0} - " +
				"CorrDiff (mm): {10:0} {11:0} {12:0} - " +
				"Rotation (°): {13:0} {14:0} {15:0} ",
				Coregistration.Correlations.Count,
				move.Position.x * 100, move.Position.y * 100, move.Position.z * 100,
				dk2.Position.x * 100, dk2.Position.y * 100, dk2.Position.z * 100,
				(move.Position.x - dk2.Position.x) * 1000,
				(move.Position.y - dk2.Position.y) * 1000,
				(move.Position.z - dk2.Position.z) * 1000,
				(CorrectedPosition.x - dk2.Position.x) * 1000,
				(CorrectedPosition.y - dk2.Position.y) * 1000,
				(CorrectedPosition.z - dk2.Position.z) * 1000,
				dk2.Orientation.eulerAngles.x, dk2.Orientation.eulerAngles.y, dk2.Orientation.eulerAngles.z
			);
			
			GUI.Label(new Rect(10, Screen.height - 20, 500, 100), display, style);
		}
	}
	
	void OnApplicationQuit() {
		if (Coregistration.Correlations.Count > 0) {
			// TODO: save transform matrix in AppData\Roaming\.psmoveapi for later use.
		} else {
			Debug.Log("No valid positions tracked, can't compute the transform matrix!");
		}
	}
}