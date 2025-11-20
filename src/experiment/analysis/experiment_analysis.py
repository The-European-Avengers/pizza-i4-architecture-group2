import pandas as pd
import matplotlib.pyplot as plt


def analyze_order_data(file_path):
    # Load data
    data = pd.read_csv(file_path)

    mean = data["LATENCYMS"].mean()
    variance = data["LATENCYMS"].var(ddof=1)   # varianza muestral
    std_dev = data["LATENCYMS"].std(ddof=1)   # desviación estándar muestral
    value_range = data["LATENCYMS"].max() - data["LATENCYMS"].min()


    print(data)
    print(f"Mean LATENCYMS: {mean}")

    plt.figure(figsize=(10, 6))
    plt.bar(data['LATENCYMS'], data['LATENCYMS'], color='orange')
    plt.xlabel('Pizza Type')
    plt.ylabel('Number of Orders')
    plt.title('Pizza Orders Analysis')
    plt.show()  
    




def analyz_pizza_data(file_path):
    # Load pizza data
    data = pd.read_csv(file_path)

    print(data)

    # Basic statistics
    summary = data.describe()
    print("Pizza Data Summary:")
    print(summary)
    
    # Plotting
    plt.figure(figsize=(10, 6))
    plt.bar(data['pizza_type'], data['orders'], color='orange')
    plt.xlabel('Pizza Type')
    plt.ylabel('Number of Orders')
    plt.title('Pizza Orders Analysis')
    plt.show()  



if __name__ == "__main__":
    analyze_order_data('C:/Users/ROGSTRIX/OneDrive/Desktop/SDU_MASTER_OFFICIAL/1st_Semester/pizza-i4-architecture-group2/src/experiment/order-data.csv')
    #analyz_pizza_data('../src/experiment/pizza_orders.csv')