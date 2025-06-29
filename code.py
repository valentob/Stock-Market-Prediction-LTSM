# -*- coding: utf-8 -*-
"""
STOCK MARKET PREDICTION
"""
import yfinance as yf
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
from sklearn.preprocessing import MinMaxScaler
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense, Dropout
from tensorflow.keras.callbacks import ModelCheckpoint, EarlyStopping
import sys

# Recibir los parámetros desde C#
dias = int(sys.argv[1])
csv_file = sys.argv[2]

# Cargar el CSV proporcionado
df = pd.read_csv(csv_file, parse_dates=['Date'], infer_datetime_format=True)

# Define the company's stock symbol
symbol = "PSLV"

# Get the object of the action
stock = yf.Ticker(symbol)

# Download historical stock data
data = stock.history(period="max", interval="1d")

# Save the data to a CSV file
data.to_csv("PSLV_data.csv")
df = pd.read_csv("PSLV_data.csv", parse_dates=['Date'], infer_datetime_format=True)

# Graph of the variable "Close" 
df['Date']=pd.to_datetime(df['Date'], utc=True)
plt.figure(figsize=[15,6])
plt.plot(df['Date'],df["Close"],marker=".")
plt.title("Closing Prices over time")
plt.xlabel('Date')
plt.ylabel("Closing Price")
plt.xticks(rotation=45)
plt.grid(True)
plt.show()

# Change the index and normalize the data
new_df=df.reset_index()['Close']
scaler = MinMaxScaler()
scaled_data = scaler.fit_transform(np.array(new_df).reshape(-1,1))

# Divide the data into train and test sets
train_size=int(len(scaled_data)*0.8) # 80% for training
train_data, test_data = scaled_data[:train_size], scaled_data[train_size:]

# Preparation of train and test data for the prediction
n_past=60 # Number of past elements to be considered for each prediction sample
x_train, y_train = [], []
for i in range(n_past, len(train_data)):
    x_train.append(train_data[i-n_past:i,0])
    y_train.append(train_data[i,0])
x_train, y_train = np.array(x_train), np.array(y_train)

x_test, y_test = [], []
for i in range(n_past, len(test_data)):
    x_test.append(test_data[i-n_past:i,0])
    y_test.append(test_data[i,0])
x_test, y_test = np.array(x_test), np.array(y_test)

# Resize the train and test sets to fit LSTM model
x_train = x_train.reshape(x_train.shape[0], x_train.shape[1], 1)
x_test = x_test.reshape(x_test.shape[0], x_test.shape[1], 1)

# LSTM model: 3 layers with 50 units each
model=Sequential()

model.add(LSTM(units=50, return_sequences=True, input_shape=[x_train.shape[1],1]))
model.add(Dropout(0.2)) #Add dropout to prevent overfitting

model.add(LSTM(units=50, return_sequences=True))
model.add(Dropout(0.2))

model.add(LSTM(units=50))
model.add(Dropout(0.2))

model.add(Dense(units=1))

# Loss and optimization functions
model.compile(loss="mean_squared_error",optimizer="adam")

# Keep the best weights during training
checkpoints= ModelCheckpoint(filepath='my_weights.keras', save_best_only=True)

# Stop training if the validation loss doesn't improve in 15 consecutive epochs
# Reset weights to the best validation loss values
early_stopping=EarlyStopping(monitor='val_loss', patience=15, restore_best_weights=True)

model.fit(x_train, y_train,
          validation_data=(x_test,y_test),
          epochs=100,
          batch_size=32,
          verbose=1,
          callbacks=[checkpoints,early_stopping])

# Predictions for the train and test dataset
train_predict=model.predict(x_train)
test_predict=model.predict(x_test)

# Convert the scale of the predictions back to their original values
train_predict=scaler.inverse_transform(train_predict)
test_predict=scaler.inverse_transform(test_predict)

# Graph of predicted values (train and test) vs actual values
look_back=60 # Number of past time values to take into account for the prediction
trainPredictPlot=np.empty_like(new_df)
trainPredictPlot[:]=np.nan
trainPredictPlot[look_back:len(train_predict)+look_back]=train_predict.flatten()

testPredictPlot=np.empty_like(new_df)
testPredictPlot[:]=np.nan
test_start=len(new_df)-len(test_predict)
testPredictPlot[test_start:]=test_predict.flatten()

original_scaled_data=scaler.inverse_transform(scaled_data)

plt.figure(figsize=(15,6))
plt.plot(original_scaled_data, color='black', label='Actual price')
plt.plot(trainPredictPlot, color='red', label='Predicted price (train set)')
plt.plot(testPredictPlot, color='blue', label='Predicted price (test set)')

plt.title("Share price")
plt.xlabel("time")
plt.ylabel("Share price")
plt.legend()
plt.show()

#Predictions for next selected days
days=10
last_sequence=x_test[-1]
last_sequence=last_sequence.reshape(1,n_past,1)
predictions_next_days=[]
for _ in range(days):
    next_day_prediction=model.predict(last_sequence)
    predictions_next_days.append(next_day_prediction[0,0]) #Get the predicted value
    last_sequence=np.roll(last_sequence, -1, axis=1) #Shift the sequence by one day
    last_sequence[0,-1,0]=next_day_prediction #Update the last element with the new prediction

predictions_next_days=scaler.inverse_transform(np.array(predictions_next_days).reshape(-1,1))

print("Predictions for the next "+str(days)+" days:")
for i, prediction in enumerate(predictions_next_days, start=1):
    print("Day "+str(i)+": Predicted price = "+str(prediction[0]))
    
plt.plot(predictions_next_days, marker='*')
plt.title("Predicted stock price for the next "+str(days)+" days")
plt.xlabel("Days")
plt.ylabel("Price")
plt.xticks(range(0, days), ["Day "+str(i+1) for i in range(days)], rotation=45)
plt.grid(True)
plt.show()

# Guardar la gráfica como archivo PNG
plt.savefig('predicted_chart.png')
plt.show()
