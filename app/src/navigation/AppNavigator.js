import React from 'react';
import { View, Text, StyleSheet, ActivityIndicator } from 'react-native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import { useAuth } from '../hooks/useAuth';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, SHADOWS } from '../constants/theme';

// Screens
import LoginScreen from '../screens/LoginScreen';
import RegisterScreen from '../screens/RegisterScreen';
import VerifyCodeScreen from '../screens/VerifyCodeScreen';
import ForgotPasswordScreen from '../screens/ForgotPasswordScreen';
import ResetPasswordScreen from '../screens/ResetPasswordScreen';
import HomeScreen from '../screens/HomeScreen';
import OrdersScreen from '../screens/OrdersScreen';
import OrderDetailScreen from '../screens/OrderDetailScreen';
import ProfileScreen from '../screens/ProfileScreen';
import PrescriptionScreen from '../screens/PrescriptionScreen';
import FormulaScreen from '../screens/FormulaScreen';
import CartScreen from '../screens/CartScreen';
import CatalogScreen from '../screens/CatalogScreen';

const Stack = createStackNavigator();
const Tab = createBottomTabNavigator();

// Auth Stack
const AuthStack = () => (
  <Stack.Navigator screenOptions={{ headerShown: false }}>
    <Stack.Screen name="Login" component={LoginScreen} />
    <Stack.Screen name="Register" component={RegisterScreen} />
    <Stack.Screen name="VerifyCode" component={VerifyCodeScreen} />
    <Stack.Screen name="ForgotPassword" component={ForgotPasswordScreen} />
    <Stack.Screen name="ResetPassword" component={ResetPasswordScreen} />
  </Stack.Navigator>
);

// Tab icon with active dot indicator
const TabIcon = ({ name, color, focused }) => (
  <View style={styles.tabIconWrapper}>
    {focused && <View style={styles.tabDot} />}
    <Feather name={name} size={22} color={color} />
  </View>
);

// Bottom Tabs
const TabNavigator = () => (
  <Tab.Navigator
    screenOptions={{
      headerShown: false,
      tabBarStyle: styles.tabBar,
      tabBarActiveTintColor: COLORS.primary,
      tabBarInactiveTintColor: COLORS.textMuted,
      tabBarLabelStyle: styles.tabLabel,
    }}
  >
    <Tab.Screen
      name="HomeTab"
      component={HomeScreen}
      options={{
        tabBarLabel: 'Início',
        tabBarIcon: ({ color, focused }) => <TabIcon name="home" color={color} focused={focused} />,
      }}
    />
    <Tab.Screen
      name="SearchTab"
      component={CatalogScreen}
      options={{
        tabBarLabel: 'Buscar',
        tabBarIcon: ({ color, focused }) => <TabIcon name="search" color={color} focused={focused} />,
      }}
    />
    <Tab.Screen
      name="OrdersTab"
      component={OrdersScreen}
      options={{
        tabBarLabel: 'Pedidos',
        tabBarIcon: ({ color, focused }) => <TabIcon name="file-text" color={color} focused={focused} />,
      }}
    />
    <Tab.Screen
      name="ProfileTab"
      component={ProfileScreen}
      options={{
        tabBarLabel: 'Perfil',
        tabBarIcon: ({ color, focused }) => <TabIcon name="user" color={color} focused={focused} />,
      }}
    />
  </Tab.Navigator>
);

// Main Stack
const MainStack = () => (
  <Stack.Navigator screenOptions={{ headerShown: false }}>
    <Stack.Screen name="MainTabs" component={TabNavigator} />
    <Stack.Screen name="Prescription" component={PrescriptionScreen} />
    <Stack.Screen name="Formula" component={FormulaScreen} />
    <Stack.Screen name="Cart" component={CartScreen} />
    <Stack.Screen name="OrderDetail" component={OrderDetailScreen} />
  </Stack.Navigator>
);

// Splash / Loading
const LoadingScreen = () => (
  <LinearGradient colors={GRADIENTS.splash} style={styles.loading}>
    <View style={styles.loadingLogoCircle}>
      <Feather name="droplet" size={40} color={COLORS.primary} />
    </View>
    <Text style={styles.loadingText}>Salvelle</Text>
    <ActivityIndicator color={COLORS.white} style={{ marginTop: 24 }} />
  </LinearGradient>
);

// Main Navigator
const AppNavigator = () => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) return <LoadingScreen />;

  return isAuthenticated ? <MainStack /> : <AuthStack />;
};

const styles = StyleSheet.create({
  tabBar: {
    position: 'absolute',
    backgroundColor: COLORS.cardGlass,
    borderTopWidth: 0,
    height: 92,
    paddingBottom: 28,
    paddingTop: 12,
    borderTopLeftRadius: BORDER_RADIUS.xxl,
    borderTopRightRadius: BORDER_RADIUS.xxl,
    ...SHADOWS.large,
  },
  tabLabel: {
    fontSize: 10,
    fontWeight: '600',
    marginTop: 2,
  },
  tabIconWrapper: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  tabDot: {
    width: 5,
    height: 5,
    borderRadius: 2.5,
    backgroundColor: COLORS.primary,
    marginBottom: 4,
  },
  loading: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingLogoCircle: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: 'rgba(255,255,255,0.95)',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: SPACING.lg,
    ...SHADOWS.medium,
  },
  loadingText: {
    fontSize: 30,
    fontWeight: '800',
    color: COLORS.white,
    letterSpacing: -0.5,
  },
});

export default AppNavigator;
