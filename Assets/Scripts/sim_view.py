import pandas as pd
import numpy as np
from scipy.spatial.transform import Rotation as R
import polyscope as ps
import polyscope.imgui as psim
import time

folder_path = 'C:\\Users\\maparicio\\Documents\\My project\\Assets\\Exports\\Prueba1\\'
faces_path = folder_path + 'Faces.csv'
left_path = folder_path + 'Left.csv'
right_path = folder_path + 'Right.csv'
mesh_path = folder_path + 'Mesh.csv'

try:
    faces = pd.read_csv(faces_path)
    left = pd.read_csv(left_path)
    right = pd.read_csv(right_path)
    mesh = pd.read_csv(mesh_path)
except:
    print("Files not found")

timestamps = left['Timestamp'].to_numpy()
positionsL = left[['X', 'Y', 'Z']].to_numpy()
positionsR = right[['X', 'Y', 'Z']].to_numpy()
rotationsL = left[['Qx', 'Qy', 'Qz', 'Qw']].to_numpy()
rotationsR = right[['Qx', 'Qy', 'Qz', 'Qw']].to_numpy()

vertices = mesh[["X", "Y", "Z"]].to_numpy()
vertices = vertices.reshape((timestamps.shape[0], -1, 3))
numVertex = vertices.shape[1]//3

faces_list = []
for index_str in faces["VertexIndices"]:
    face = [int(idx) for idx in index_str.split(";")]
    faces_list = []
    faces_list.append(face)

faces = np.array([ [int(idx) for idx in row.split(";")] for row in faces["VertexIndices"] ])

timestamps = timestamps[:] - timestamps[0]

gripper_local_nodes = np.array([
    [0.0, 0.0, 0.0],      # Node 0: Wrist joint base
    [-0.04, 0.0, 0.0],    # Node 1: Left base corner
    [0.04, 0.0, 0.0],     # Node 2: Right base corner
    [-0.04, 0.0, 0.06],   # Node 3: Left gripper finger tip
    [0.04, 0.0, 0.06],    # Node 4: Right gripper finger tip
])

gripper_edges = np.array([
    [0, 1], [0, 2],       # Crossbar connection
    [1, 3], [2, 4]        # Extended parallel fingers
])

ps.init()
#ps.register_curve_network("Trajectory Trace Left", positionsL, edges='line', color=(0.4, 0.4, 0.4), radius=0.001)
#ps.register_curve_network("Trajectory Trace Right", positionsR, edges='line', color=(0.4, 0.4, 0.4), radius=0.001)

ps_gripper_left = ps.register_curve_network("Robotic Gripper Left", gripper_local_nodes, gripper_edges, color=(1.0, 0.35, 0.0), radius=0.004)
ps_gripper_right = ps.register_curve_network("Robotic Gripper Right", gripper_local_nodes, gripper_edges, color=(1.0, 0.35, 0.0), radius=0.004)
mesh = ps.register_surface_mesh("Loaded_Mesh", vertices[0], faces)

frame_idx = 0
is_playing = True
last_update_time = time.time()
playback_speed_fps = 30.0

def animation_callback():
    global frame_idx, is_playing, last_update_time, playback_speed_fps
    
    psim.Text("Gripper Telemetry Controls")
    
    _, is_playing = psim.Checkbox("Play Animation", is_playing)

    changed_slider, new_frame = psim.SliderInt("Frame Timeline", frame_idx, 0, len(positionsL) - 1)
    if changed_slider:
        frame_idx = new_frame
        is_playing = False  # Pause playback immediately when dragging manual frame slider
        
    _, playback_speed_fps = psim.SliderFloat("Speed (FPS)", playback_speed_fps, 1.0, 120.0)
    
    psim.Separator()
    psim.Text(f"Active Frame: {frame_idx} / {len(positionsL) - 1}")
    psim.Text(f"XYZ Position Left: [{positionsL[frame_idx][0]:.3f}, {positionsL[frame_idx][1]:.3f}, {positionsL[frame_idx][2]:.3f}]")
    psim.Text(f"XYZ Position Right: [{positionsR[frame_idx][0]:.3f}, {positionsR[frame_idx][1]:.3f}, {positionsR[frame_idx][2]:.3f}]")
    
    current_time = time.time()
    if is_playing:
        elapsed = current_time - last_update_time
        frames_to_skip = int(elapsed * playback_speed_fps)
        if frames_to_skip > 0:
            frame_idx = (frame_idx + frames_to_skip) % len(positionsL)
            last_update_time = current_time
    else:
        last_update_time = current_time
    
    # left controller
    pos = positionsL[frame_idx]
    rot = rotationsL[frame_idx]
    
    r = R.from_quat(rot)
    rot_matrix = r.as_matrix()
    rot_gripper = np.array([gripper_local_nodes[i] @ rot_matrix.T for i in range(5)])
    transformed_nodes = (rot_gripper) + pos
    
    ps_gripper_left.update_node_positions(transformed_nodes)

    # right controller
    pos = positionsR[frame_idx]
    rot = rotationsR[frame_idx]
    
    r = R.from_quat(rot)
    rot_matrix = r.as_matrix()
    rot_gripper = np.array([gripper_local_nodes[i] @ rot_matrix.T for i in range(5)])
    transformed_nodes = (rot_gripper) + pos
    
    ps_gripper_right.update_node_positions(transformed_nodes)
    mesh.update_vertex_positions(vertices[frame_idx])


ps.set_user_callback(animation_callback)

ps.show()
