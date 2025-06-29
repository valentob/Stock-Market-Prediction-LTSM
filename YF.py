import yfinance as yf

# Definir el símbolo de la acción de la empresa (por ejemplo, Apple - AAPL)
symbol = "GME"

# Obtener el objeto de la acción
stock = yf.Ticker(symbol)

# Descargar los datos históricos de la acción
data = stock.history(period="max", interval="1d")

# Mostrar los primeros registros
print(data.head())

# Guardar los datos en un archivo CSV
data.to_csv("GME_data.csv")
