import { initializeApp } from 'firebase/app';
import {
  browserLocalPersistence,
  getAuth,
  onAuthStateChanged,
  setPersistence,
  signInWithEmailAndPassword,
  signOut
} from 'firebase/auth';
import { doc, getDoc, getFirestore, onSnapshot } from 'firebase/firestore';

const firebaseConfig = {
  apiKey: 'AIzaSyBWZMFfJFwKeCx5yIys1x4n88QPNB_F668',
  authDomain: 'viniedosdelavilla.firebaseapp.com',
  projectId: 'viniedosdelavilla',
  storageBucket: 'viniedosdelavilla.firebasestorage.app',
  messagingSenderId: '239998319399',
  appId: '1:239998319399:web:40d61346e99cd7f8228702'
};

const firebaseApp = initializeApp(firebaseConfig);
const firebaseAuth = getAuth(firebaseApp);
const firestore = getFirestore(firebaseApp);

const persistenceReady = setPersistence(firebaseAuth, browserLocalPersistence).catch(() => undefined);

export {
  doc,
  firebaseAuth,
  firestore,
  getDoc,
  onAuthStateChanged,
  onSnapshot,
  signInWithEmailAndPassword,
  signOut
};

export const ensureFirebasePersistence = () => persistenceReady;
