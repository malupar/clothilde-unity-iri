import sys,os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))
from Cloth import Cloth 
from utils import createRectangularMesh, createRectangularCurvedMesh
import scipy.io
import numpy as np
import time

# Caida libre
X, Tq = createRectangularMesh(a = 0.5,b = 0.5,na = 12,nb = 12)
X[:,2] += 0.25
mesh = Cloth(X, Tq, corners=[0, 11, 11*12, 12*12-1])
#plt.spy(mesh.M_lum)
#mesh.Fg
mesh.mu = 0
start_time = time.time()
for i in range(100):
    print(i)
    mesh.simulate(dt = 0.01, u = X[0], control = [0], tolerance = 0.01)
print(time.time()-start_time)

mesh.makeMovie()