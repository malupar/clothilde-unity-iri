from ClothF import Cloth 
from utils import createRectangularMesh
import numpy as np

# Caida libre
na = 15; nb = 15
X, T = createRectangularMesh(a = 0.8, b = 0.8, na = na, nb = nb, h = 0.1)
self = Cloth(X, T)
dt = 1/60
print(dt)
self.setSimulatorParameters(dt = dt, thck = 0.99, mu_s = 0.35, str = 0.001*1e-4, shr = 2.5*1e-4, 
                            tol = 0.0075, kappa = 1.5*1e-4, kappa_bnd = 0.5*1e-4,  mu_f = 0.25, sub_steps = 10, cusick = True)
self.plotMesh()

tf = int(3/dt); t = np.linspace(0,2*np.pi,tf); freq = 2
inds = []
u = self.positions[inds]
for i in range(tf):
    self.simulate(inds, u)

self.makeMovie(speed=1,repeat=False,smooth=2)