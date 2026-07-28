import React, { useState, useRef } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Alert,
  ActivityIndicator,
  Image,
  TextInput,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import * as ImageManipulator from 'expo-image-manipulator';
import * as api from '../services/api';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const PrescriptionScreen = ({ navigation }) => {
  const [image, setImage] = useState(null);
  const [imageBase64, setImageBase64] = useState(null);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [ocrResult, setOcrResult] = useState(null);
  const [observations, setObservations] = useState('');

  const takePhoto = async () => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permissão necessária', 'Precisamos de acesso à câmera para tirar fotos da receita.');
      return;
    }

    const result = await ImagePicker.launchCameraAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      quality: 0.8,
      base64: true,
    });

    if (!result.canceled && result.assets[0]) {
      await processImage(result.assets[0]);
    }
  };

  const pickImage = async () => {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permissão necessária', 'Precisamos de acesso à galeria para selecionar fotos.');
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      quality: 0.8,
      base64: true,
    });

    if (!result.canceled && result.assets[0]) {
      await processImage(result.assets[0]);
    }
  };

  const processImage = async (asset) => {
    setLoading(true);
    try {
      const compressed = await ImageManipulator.manipulateAsync(
        asset.uri,
        [{ resize: { width: 1200 } }],
        { compress: 0.7, format: ImageManipulator.SaveFormat.JPEG, base64: true }
      );

      setImage(compressed.uri);
      setImageBase64(compressed.base64);
      setOcrResult(null);
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível processar a imagem.');
    } finally {
      setLoading(false);
    }
  };

  const uploadPrescription = async () => {
    if (!imageBase64) {
      Alert.alert('Atenção', 'Selecione ou tire uma foto da receita primeiro.');
      return;
    }

    setUploading(true);
    setUploadProgress(0);

    const progressInterval = setInterval(() => {
      setUploadProgress(prev => Math.min(prev + 10, 90));
    }, 300);

    try {
      const result = await api.uploadPrescription(imageBase64, observations);

      clearInterval(progressInterval);
      setUploadProgress(100);

      if (result.success) {
        setOcrResult(result);
        Alert.alert(
          'Receita enviada!',
          'Sua receita foi processada com sucesso. Aguarde o orçamento.',
          [
            { text: 'Ver orçamento', onPress: () => navigation.navigate('Orders') },
            { text: 'OK' }
          ]
        );
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível processar a receita.');
      }
    } catch (error) {
      clearInterval(progressInterval);
      Alert.alert('Erro', 'Não foi possível enviar a receita. Tente novamente.');
    } finally {
      setUploading(false);
      setUploadProgress(0);
    }
  };

  const resetForm = () => {
    setImage(null);
    setImageBase64(null);
    setOcrResult(null);
    setObservations('');
  };

  return (
    <LinearGradient colors={GRADIENTS.background} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
          <View style={styles.backButtonCircle}>
            <Feather name="arrow-left" size={20} color={COLORS.primary} />
          </View>
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Enviar Receita</Text>
        <View style={{ width: 44 }} />
      </View>

      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {/* Instruction card */}
        <View style={styles.instructionCard}>
          <View style={styles.instructionAccent} />
          <View style={styles.instructionBody}>
            <Text style={styles.instructionTitle}>Como funciona?</Text>

            <View style={styles.stepRow}>
              <View style={styles.stepCircle}><Text style={styles.stepNumber}>1</Text></View>
              <Text style={styles.stepText}>Tire uma foto ou selecione a receita</Text>
            </View>

            <View style={styles.stepRow}>
              <View style={styles.stepCircle}><Text style={styles.stepNumber}>2</Text></View>
              <Text style={styles.stepText}>Nossa IA extrai os dados automaticamente</Text>
            </View>

            <View style={styles.stepRow}>
              <View style={styles.stepCircle}><Text style={styles.stepNumber}>3</Text></View>
              <Text style={styles.stepText}>Receba o orçamento em minutos!</Text>
            </View>
          </View>
        </View>

        {/* Image area */}
        <View style={styles.imageSection}>
          {image ? (
            <View style={styles.imagePreview}>
              <Image source={{ uri: image }} style={styles.previewImage} />
              <TouchableOpacity style={styles.removeImageButton} onPress={resetForm}>
                <Feather name="x" size={18} color={COLORS.white} />
              </TouchableOpacity>
            </View>
          ) : (
            <View style={styles.imagePlaceholder}>
              <View style={styles.cameraIconWrapper}>
                <Feather name="camera" size={48} color={COLORS.primary} />
              </View>
              <Text style={styles.placeholderText}>Sua receita aparecerá aqui</Text>
            </View>
          )}
        </View>

        {/* Capture buttons */}
        {!image && (
          <View style={styles.captureButtons}>
            <TouchableOpacity style={styles.captureButton} onPress={takePhoto} disabled={loading}>
              <LinearGradient colors={GRADIENTS.primary} style={styles.captureButtonGradient}>
                {loading ? (
                  <ActivityIndicator color={COLORS.white} />
                ) : (
                  <>
                    <Feather name="camera" size={22} color={COLORS.white} />
                    <Text style={styles.captureButtonText}>Tirar foto</Text>
                  </>
                )}
              </LinearGradient>
            </TouchableOpacity>

            <TouchableOpacity style={styles.captureButtonOutline} onPress={pickImage} disabled={loading}>
              <Feather name="image" size={22} color={COLORS.primary} />
              <Text style={styles.captureButtonOutlineText}>Galeria</Text>
            </TouchableOpacity>
          </View>
        )}

        {/* Observations */}
        {image && (
          <View style={styles.observationsSection}>
            <Text style={styles.sectionLabel}>Observações (opcional)</Text>
            <TextInput
              style={styles.observationsInput}
              placeholder="Ex: Urgente, preciso para semana que vem..."
              placeholderTextColor={COLORS.textMuted}
              value={observations}
              onChangeText={setObservations}
              multiline
              numberOfLines={3}
              textAlignVertical="top"
            />
          </View>
        )}

        {/* Progress bar */}
        {uploading && (
          <View style={styles.progressSection}>
            <Text style={styles.progressText}>Enviando... {uploadProgress}%</Text>
            <View style={styles.progressBar}>
              <LinearGradient
                colors={GRADIENTS.primary}
                start={{ x: 0, y: 0 }}
                end={{ x: 1, y: 0 }}
                style={[styles.progressFill, { width: `${uploadProgress}%` }]}
              />
            </View>
          </View>
        )}

        {/* Submit button */}
        {image && !uploading && (
          <TouchableOpacity onPress={uploadPrescription} activeOpacity={0.8}>
            <LinearGradient colors={GRADIENTS.primary} style={styles.submitButton}>
              <Feather name="send" size={20} color={COLORS.white} />
              <Text style={styles.submitButtonText}>Enviar para orçamento</Text>
            </LinearGradient>
          </TouchableOpacity>
        )}

        {/* OCR results */}
        {ocrResult && ocrResult.medications && (
          <View style={styles.ocrResultSection}>
            <Text style={styles.sectionLabel}>Itens identificados</Text>
            {ocrResult.medications.map((med, index) => (
              <View key={index} style={styles.medicationItem}>
                <View style={styles.medicationCheck}>
                  <Feather name="check" size={14} color={COLORS.white} />
                </View>
                <Text style={styles.medicationText}>{med.name}</Text>
                <Text style={styles.medicationDose}>{med.dose}</Text>
              </View>
            ))}
          </View>
        )}

        <View style={{ height: 100 }} />
      </ScrollView>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1 },
  header: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingTop: SPACING.xxxl + 10, paddingHorizontal: SPACING.lg, paddingBottom: SPACING.md,
  },
  backButton: { padding: 2 },
  backButtonCircle: {
    width: 44, height: 44, borderRadius: 22,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
  },
  headerTitle: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.text },
  scrollView: { flex: 1 },
  scrollContent: { padding: SPACING.lg },

  instructionCard: {
    flexDirection: 'row', backgroundColor: COLORS.cardGlass, borderRadius: BORDER_RADIUS.xxl,
    overflow: 'hidden', marginBottom: SPACING.xl, borderWidth: 1, borderColor: COLORS.borderLight, ...SHADOWS.small,
  },
  instructionAccent: { width: 5, backgroundColor: COLORS.primary },
  instructionBody: { flex: 1, padding: SPACING.lg },
  instructionTitle: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.text, marginBottom: SPACING.md },
  stepRow: { flexDirection: 'row', alignItems: 'center', marginBottom: SPACING.sm },
  stepCircle: {
    width: 28, height: 28, borderRadius: 14, backgroundColor: COLORS.primary,
    alignItems: 'center', justifyContent: 'center', marginRight: SPACING.sm,
  },
  stepNumber: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.white },
  stepText: { flex: 1, fontSize: FONT_SIZES.sm, color: COLORS.textSecondary, lineHeight: 20 },

  imageSection: { marginBottom: SPACING.xl },
  imagePlaceholder: {
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl, height: 250,
    alignItems: 'center', justifyContent: 'center', borderWidth: 2, borderStyle: 'dashed', borderColor: COLORS.primary,
  },
  cameraIconWrapper: {
    width: 88, height: 88, borderRadius: 44, backgroundColor: COLORS.primaryMuted,
    alignItems: 'center', justifyContent: 'center', marginBottom: SPACING.md,
  },
  placeholderText: { fontSize: FONT_SIZES.md, color: COLORS.textMuted },
  imagePreview: { position: 'relative', borderRadius: BORDER_RADIUS.xxl, overflow: 'hidden' },
  previewImage: { width: '100%', height: 300, borderRadius: BORDER_RADIUS.xxl },
  removeImageButton: {
    position: 'absolute', top: SPACING.sm, right: SPACING.sm,
    backgroundColor: COLORS.error, borderRadius: 20, padding: SPACING.sm,
  },

  captureButtons: { flexDirection: 'row', gap: SPACING.md, marginBottom: SPACING.xl },
  captureButton: {
    flex: 1, borderRadius: BORDER_RADIUS.xxl, overflow: 'hidden',
    ...SHADOWS.colored(COLORS.primary),
  },
  captureButtonGradient: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    paddingVertical: 16, paddingHorizontal: SPACING.lg, gap: SPACING.sm,
  },
  captureButtonText: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.white },
  captureButtonOutline: {
    flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    paddingVertical: 16, paddingHorizontal: SPACING.lg, gap: SPACING.sm,
    borderRadius: BORDER_RADIUS.xxl, borderWidth: 2, borderColor: COLORS.primary, backgroundColor: COLORS.white,
  },
  captureButtonOutlineText: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.primary },

  observationsSection: { marginBottom: SPACING.xl },
  sectionLabel: { fontSize: FONT_SIZES.md, fontWeight: '600', color: COLORS.text, marginBottom: SPACING.sm },
  observationsInput: {
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.lg, padding: SPACING.md,
    fontSize: FONT_SIZES.md, color: COLORS.text, minHeight: 80, borderWidth: 1, borderColor: COLORS.border,
  },

  progressSection: { marginBottom: SPACING.xl },
  progressText: { fontSize: FONT_SIZES.sm, color: COLORS.textSecondary, marginBottom: SPACING.sm, textAlign: 'center', fontWeight: '600' },
  progressBar: { height: 10, backgroundColor: COLORS.borderLight, borderRadius: 5, overflow: 'hidden' },
  progressFill: {
    height: '100%', borderRadius: 5,
    ...SHADOWS.glow(COLORS.primary),
  },

  submitButton: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    paddingVertical: 18, paddingHorizontal: SPACING.xl, borderRadius: BORDER_RADIUS.xxl, gap: SPACING.sm,
    ...SHADOWS.colored(COLORS.primary),
  },
  submitButtonText: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.white },

  ocrResultSection: {
    marginTop: SPACING.xl, backgroundColor: COLORS.cardGlass, borderRadius: BORDER_RADIUS.xxl,
    padding: SPACING.lg, borderWidth: 1, borderColor: COLORS.borderLight,
  },
  medicationItem: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.sm,
    paddingVertical: SPACING.sm + 2, borderBottomWidth: 1, borderBottomColor: COLORS.borderLight,
  },
  medicationCheck: {
    width: 24, height: 24, borderRadius: 12, backgroundColor: COLORS.success,
    alignItems: 'center', justifyContent: 'center',
  },
  medicationText: { flex: 1, fontSize: FONT_SIZES.md, color: COLORS.text, fontWeight: '500' },
  medicationDose: { fontSize: FONT_SIZES.sm, color: COLORS.textMuted, fontWeight: '500' },
});

export default PrescriptionScreen;
