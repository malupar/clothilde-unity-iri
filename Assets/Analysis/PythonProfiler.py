import pandas as pd 
import matplotlib.pyplot as plt

folder_path = 'C:\\Users\\apari\\Documents\\Github\\clothilde-unity-iri\\Assets\\Exports\\Session_20260630_010902\\'
file_path = folder_path + "export.csv"
df = pd.read_csv(file_path)

plt.figure(figsize=(12, 6))
plt.xlabel("Llamadas a Python")
plt.ylabel("Segundos")
plt.plot(df["T"], df["Elapsed"], marker='o', linestyle='-', color='b', label="Conexion+simulate")
plt.plot(df["T"], df["Simulate"], marker='o', linestyle='-', color='r', label="Simulate")
plt.plot(df["T"], df["Elapsed"] - df["Simulate"], marker='o', linestyle='-', color='g', label="Conexion")
plt.legend()