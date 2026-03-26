# Assets/Scripts/

The following scripts have been developped:

## ColorChange.cs

If this component is added to the mesh object in Unity you may change the texture of the cloth by clicking the key 'C'.

## DragController.cs

If this component is added to the controllers object you will be able to interact with the DragHandle object (and therefore the mesh) using the SteamVR plugin.

The key function in this class is **Update** where we iterate over all DragHandle objects and choose the one that is closer to a certain point of the controller.

## DragHandle.cs

If this component is added to a Unity object it will enable interaction using the mouse or the controllers.

This class only has 3 types of functions:
- Select this DragHandle object.
- Unselect this DragHandle object.
- Move this selected object to this position.

## Gradient generator.cs

Class developped in order to generate gradient textures.

## JsonExporter.cs

If this component is added to *any* object in the scene then the position of all objects with the **Exportable** tag will be able to be recorded in the Assets/Exports/ folder.

## PythonConnection.cs

This class makes sure that the connection with the Python environment is correct. You should only modify this file while working on the setup for this project.

## TriangleMesh.cs

This class takes charge of calling the Python simulator, generating and handling the mesh as well as the handle (DragHandle) objects you may interact with.

- **CreateHandle** and **CreateGrid** take care of the generation of the mesh and the DragHandle objects.
  - A mesh with a chosen resolution of $n_x$ vertices wide and $n_y$ vertices long will also generate another vertex in the middle of each quadrangular face. The number of nodes are also doubled in order to color both sides of the cloth with different textures.
  - The quadrangular faces are broken down into 4 triangular faces (using the center node mentioned above).
  - The number of handles can be modified in the idxArray variable. You should assign the index of the vertex which you would like to associate with a handle. Currently a handle is generated for each original vertex (not counting center vertex and copies).
- **getMeshPositions**, **getTotalMeshPositions**, **getFaces**, **getFacesTri** are getter functions.
- **updateHandlePosition**, **DragHandle** and **LetHandleGo** take care of the handle positions and movement.
- **loadPositionsFromMesh** updates the position of the nodes from the Python simulator to the nodes in Unity.
- **Update** calls the Python simulator with the selected vertex and positions.

The workflow of a controller dragging a vertex would be the following:

- The controller button is pushed.
- The Update function on DragController is called.
- A vertex is selected and if the distance is close enough the function HandSelected from DragHandle is called.
- If a vertex has been selected the HandDrag function from DragHandle is called.
- Then the DragHandle function from TriangleMesh gets called and this vertex gets added to the simulated map with the desired next position.
- Then the Update function from TriangleMesh is called and the Python simulator gets executed, finally the positions are loaded and updated in Unity.
