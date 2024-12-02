import logo from './logo.svg';
import './App.css';
import HomePage from './components/HomePage';
import OptionsPlaces from './components/OptionsPlaces';
import Try from './components/Try';
import ResultsPage from './components/ResultsPage';
import React, { useState } from 'react';


function App() {
  const [currentPage, setCurrentPage] = useState('home');
  const [pathData, setPathData] = useState(null);

  return (
    <div className="App">
      {currentPage === 'home' && <HomePage setCurrentPage={setCurrentPage} setPathData={setPathData} />}
      {currentPage === 'results' && <ResultsPage pathData={pathData} setCurrentPage={setCurrentPage} />}
    </div>
  );
}


// function App() {
//   return (
//     <div className="App">
//      { <HomePage/> }
//      {<ResultsPage/>}
//      {/* <Try/> */}
//      {/* <OptionsPlaces/> */}
    
//     </div>
//   );
// }

export default App;
